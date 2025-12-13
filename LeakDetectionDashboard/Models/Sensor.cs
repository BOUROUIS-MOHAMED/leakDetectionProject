namespace LeakDetectionDashboard.Models;

public class Sensor
{
    public int Id { get; set; }                 // From IoT JSON
    public string Name { get; set; } = string.Empty;

    public int PipeId { get; set; }
    public Pipe? Pipe { get; set; }

    public int? PreviousSensorId { get; set; }
    public Sensor? PreviousSensor { get; set; }

    public string Location { get; set; } = string.Empty;
    public double DistanceFromPreviousSensor { get; set; }
    public double Elevation { get; set; }
    public bool IsWaterTap { get; set; }
    public double ExpectedDailyUsage { get; set; }
    public string SensorStatus { get; set; } = "active";
    public DateTime LastCalibrationDate { get; set; }

    public ICollection<SensorSnapshot> Snapshots { get; set; } = new List<SensorSnapshot>();
}
