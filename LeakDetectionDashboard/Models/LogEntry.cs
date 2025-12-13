namespace LeakDetectionDashboard.Models;

public class LogEntry
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Level { get; set; } = "Info";
    public string Message { get; set; } = string.Empty;
    public string? Context { get; set; }
}
