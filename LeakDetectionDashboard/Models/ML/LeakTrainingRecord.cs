using Microsoft.ML.Data;

namespace LeakDetectionDashboard.Models.ML;

public class LeakTrainingRecord
{
    // 0: timestamp (we won't use as feature here)
    [LoadColumn(0)]
    public string Timestamp { get; set; } = string.Empty;

    [LoadColumn(1)]
    public float SensorId { get; set; }

    [LoadColumn(2)]
    public float PipeId { get; set; }

    [LoadColumn(3)]
    public float ZoneId { get; set; }

    [LoadColumn(4)]
    public float PressureCurrent { get; set; }

    [LoadColumn(5)]
    public float PressurePreviousSensor { get; set; }

    [LoadColumn(6)]
    public float FlowRate { get; set; }

    [LoadColumn(7)]
    public float WaterUsageDiff { get; set; }

    [LoadColumn(8)]
    public float PressureDropRate { get; set; }

    [LoadColumn(9)]
    public float Hour { get; set; }

    [LoadColumn(10)]
    public float Minute { get; set; }

    [LoadColumn(11)]
    public float DayOfWeek { get; set; }

    [LoadColumn(12)]
    public float IsWorkingHours { get; set; }

    [LoadColumn(13)]
    public float IsBreakTime { get; set; }

    [LoadColumn(14)]
    public string BreakType { get; set; } = string.Empty;

    [LoadColumn(15)]
    public float ExpectedUsageMultiplier { get; set; }

    [LoadColumn(16)]
    public float MinutesSinceBreakStart { get; set; }

    [LoadColumn(17)]
    public float OccupancyLevel { get; set; }

    [LoadColumn(18)]
    public float PressureCurrentVsBaseline { get; set; }

    [LoadColumn(19)]
    public float FlowRateVsBaseline { get; set; }

    [LoadColumn(20), ColumnName("Label")]
    public bool IsLeak { get; set; }

    [LoadColumn(21)]
    public float LeakSeverity { get; set; }

    [LoadColumn(22)]
    public string LeakType { get; set; } = string.Empty;
}
