namespace LeakDetectionDashboard.Models;

public class SensorSnapshot
{
    public int Id { get; set; }

    public DateTime Timestamp { get; set; }

    public int SensorId { get; set; }
    public Sensor? Sensor { get; set; }

    public int PipeId { get; set; }
    public int ZoneId { get; set; }

    // Features used by ML model (flattened from JSON + schedule)
    public double PressureCurrent { get; set; }
    public double PressurePreviousSensor { get; set; }
    public double FlowRate { get; set; }
    public double WaterUsageDiff { get; set; }
    public double PressureDropRate { get; set; }

    public int Hour { get; set; }
    public int Minute { get; set; }
    public int DayOfWeek { get; set; }
    public bool IsWorkingHours { get; set; }
    public bool IsBreakTime { get; set; }
    public string BreakType { get; set; } = "none";
    public double ExpectedUsageMultiplier { get; set; }
    public int MinutesSinceBreakStart { get; set; }
    public double OccupancyLevel { get; set; }
    public double PressureCurrentVsBaseline { get; set; }
    public double FlowRateVsBaseline { get; set; }

    // Outputs from ML model
    public double LeakProbability { get; set; }  // 0..1
    public bool IsLeakPredicted { get; set; }
    public int LeakSeverityPredicted { get; set; }
    public string LeakTypePredicted { get; set; } = "none";
}
