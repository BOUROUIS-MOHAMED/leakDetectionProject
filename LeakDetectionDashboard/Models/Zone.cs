namespace LeakDetectionDashboard.Models;

public class Zone
{
    public int Id { get; set; }                // From IoT JSON
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "normal";

    public ICollection<Pipe> Pipes { get; set; } = new List<Pipe>();
}
