namespace LeakDetectionDashboard.Models;

public class LeakPredictionResult
{
    public int PipeId { get; set; }
    public string PipeName { get; set; } = string.Empty;

    public int ZoneId { get; set; }
    public string ZoneName { get; set; } = string.Empty;

    /// <summary>
    /// Leak probability in [0..1].
    /// </summary>
    public double LeakProbability { get; set; }

    public string RiskLabel
    {
        get
        {
            var p = LeakProbability * 100.0;
            return p switch
            {
                <= 20 => "Normal",
                <= 40 => "High usage",
                <= 60 => "Risk of leak",
                <= 80 => "High possibility",
                _ => "Leak"
            };
        }
    }
}
