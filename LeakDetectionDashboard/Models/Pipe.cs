namespace LeakDetectionDashboard.Models;

public class Pipe
{
    public int Id { get; set; }                 // From IoT JSON
    public string Name { get; set; } = string.Empty;

    public int ZoneId { get; set; }
    public Zone? Zone { get; set; }

    public int? PreviousPipeId { get; set; }
    public Pipe? PreviousPipe { get; set; }

    public double Diameter { get; set; }
    public double Length { get; set; }
    public string Material { get; set; } = string.Empty;
    public DateTime InstallationDate { get; set; }
    public double ExpectedPressureDrop { get; set; }

    public ICollection<Sensor> Sensors { get; set; } = new List<Sensor>();
}
