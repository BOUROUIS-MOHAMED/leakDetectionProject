using Microsoft.ML.Data;

namespace LeakDetectionDashboard.Models.ML;

public class LeakPrediction
{
    [ColumnName("PredictedLabel")]
    public bool PredictedLeak { get; set; }

    public float Probability { get; set; }

    public float Score { get; set; }
}
