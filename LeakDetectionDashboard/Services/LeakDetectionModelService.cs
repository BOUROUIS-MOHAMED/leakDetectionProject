using LeakDetectionDashboard.Models.ML;
using Microsoft.ML;

namespace LeakDetectionDashboard.Services;

public class LeakDetectionModelService
{
    private readonly IConfiguration _configuration;
    private readonly object _lock = new();
    private MLContext _mlContext = new();
    private ITransformer? _model;

    public LeakDetectionModelService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private string TrainingDataPath =>
        _configuration["ML:TrainingDataPath"] ?? "Data/training/leak_training_data.csv";

    private string ModelPath =>
        _configuration["ML:ModelPath"] ?? "Data/models/leak_detection_model.zip";

    public void EnsureModelLoaded()
    {
        lock (_lock)
        {
            if (_model != null)
                return;

            if (File.Exists(ModelPath))
            {
                using var fileStream = File.OpenRead(ModelPath);
                _model = _mlContext.Model.Load(fileStream, out _);
            }
            else
            {
                TrainModel();
            }
        }
    }

    public void TrainModel()
    {
        lock (_lock)
        {
            if (!File.Exists(TrainingDataPath))
            {
                throw new FileNotFoundException($"Training data not found at {TrainingDataPath}");
            }

            var dataView = _mlContext.Data.LoadFromTextFile<LeakTrainingRecord>(
                path: TrainingDataPath,
                hasHeader: true,
                separatorChar: ',');

            // Convert BreakType and LeakType (categorical) to key
            var pipeline = _mlContext.Transforms.Text.FeaturizeText("BreakTypeFeaturized", nameof(LeakTrainingRecord.BreakType))
                .Append(_mlContext.Transforms.Text.FeaturizeText("LeakTypeFeaturized", nameof(LeakTrainingRecord.LeakType)))
                .Append(_mlContext.Transforms.Concatenate("Features",
                    nameof(LeakTrainingRecord.PressureCurrent),
                    nameof(LeakTrainingRecord.PressurePreviousSensor),
                    nameof(LeakTrainingRecord.FlowRate),
                    nameof(LeakTrainingRecord.WaterUsageDiff),
                    nameof(LeakTrainingRecord.PressureDropRate),
                    nameof(LeakTrainingRecord.Hour),
                    nameof(LeakTrainingRecord.Minute),
                    nameof(LeakTrainingRecord.DayOfWeek),
                    nameof(LeakTrainingRecord.IsWorkingHours),
                    nameof(LeakTrainingRecord.IsBreakTime),
                    nameof(LeakTrainingRecord.ExpectedUsageMultiplier),
                    nameof(LeakTrainingRecord.MinutesSinceBreakStart),
                    nameof(LeakTrainingRecord.OccupancyLevel),
                    nameof(LeakTrainingRecord.PressureCurrentVsBaseline),
                    nameof(LeakTrainingRecord.FlowRateVsBaseline),
                    "BreakTypeFeaturized",
                    "LeakTypeFeaturized"))
                .Append(_mlContext.BinaryClassification.Trainers.FastTree());

            var model = pipeline.Fit(dataView);

            Directory.CreateDirectory(Path.GetDirectoryName(ModelPath)!);
            using var fs = File.Create(ModelPath);
            _mlContext.Model.Save(model, dataView.Schema, fs);

            _model = model;
        }
    }

    public double PredictLeakProbability(LeakTrainingRecord features)
    {
        EnsureModelLoaded();

        if (_model == null)
            throw new InvalidOperationException("Model not loaded");

        // Build a prediction engine per call; small overhead, thread-safe
        var engine = _mlContext.Model.CreatePredictionEngine<LeakTrainingRecord, LeakPrediction>(_model);
        var prediction = engine.Predict(features);

        return prediction.Probability;
    }
}
