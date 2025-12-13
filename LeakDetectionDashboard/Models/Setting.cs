namespace LeakDetectionDashboard.Models;

public class Setting
{
    public int Id { get; set; }

    /// <summary>
    /// Poll interval (and history window) in minutes for reading from Fake IoT backend.
    /// </summary>
    public int PollIntervalMinutes { get; set; } = 5;
}
