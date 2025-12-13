using LeakDetectionDashboard.Data;
using LeakDetectionDashboard.Models;

namespace LeakDetectionDashboard.Services;

public class LoggingService
{
    private readonly AppDbContext _db;
    private readonly ILogger<LoggingService> _logger;

    public LoggingService(AppDbContext db, ILogger<LoggingService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task LogAsync(string level, string message, string? context = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[{Level}] {Message}", level, message);

        _db.LogEntries.Add(new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = level,
            Message = message,
            Context = context
        });

        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task LogInfoAsync(string message, string? context = null, CancellationToken cancellationToken = default)
        => LogAsync("Info", message, context, cancellationToken);

    public Task LogWarningAsync(string message, string? context = null, CancellationToken cancellationToken = default)
        => LogAsync("Warning", message, context, cancellationToken);

    public Task LogErrorAsync(string message, string? context = null, CancellationToken cancellationToken = default)
        => LogAsync("Error", message, context, cancellationToken);
}
