using LeakDetectionDashboard.Data;
using LeakDetectionDashboard.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LeakDetectionDashboard.Background;

public class SensorPollingService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SensorPollingService> _logger;

    public SensorPollingService(IServiceScopeFactory scopeFactory, ILogger<SensorPollingService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Sensor polling service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            int minutes = 5;

            try
            {
                using var scope = _scopeFactory.CreateScope();

                var settingsService = scope.ServiceProvider.GetRequiredService<SettingsService>();
                var fakeClient = scope.ServiceProvider.GetRequiredService<FakeIotClient>();
                var leakService = scope.ServiceProvider.GetRequiredService<LeakDetectionService>();
                var loggingService = scope.ServiceProvider.GetRequiredService<LoggingService>();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // 1) Poll interval from settings
                minutes = await settingsService.GetPollIntervalMinutesAsync(stoppingToken);
                minutes = Math.Max(1, minutes); // safety

                _logger.LogInformation("Polling IoT backend with window {Minutes} minutes", minutes);

                // 2) Call IoT backend
                var payload = await fakeClient.GetReadingsAsync(minutes, stoppingToken);

                if (payload == null)
                {
                    _logger.LogWarning("GetReadingsAsync returned null; no IoT data this cycle.");
                    await loggingService.LogWarningAsync("SensorPollingService: failed to fetch IoT readings");
                }
                else
                {
                    _logger.LogInformation(
                        "Received IoT payload: {ZoneCount} zones, {PipeCount} pipes, {SensorCount} sensors",
                        payload.Zones?.Count ?? 0,
                        payload.Pipes?.Count ?? 0,
                        payload.Sensors?.Count ?? 0
                    );

                    // 3) Count snapshots before processing
                    var beforeSnapshots = await dbContext.SensorSnapshots.CountAsync(stoppingToken);

                    // 4) Process & persist into DB
                    await leakService.ProcessReadingAsync(payload, stoppingToken);

                    // 5) Count snapshots after processing
                    var afterSnapshots = await dbContext.SensorSnapshots.CountAsync(stoppingToken);
                    var added = afterSnapshots - beforeSnapshots;

                    _logger.LogInformation(
                        "LeakDetectionService processed payload. SensorSnapshots before={Before}, after={After}, added={Added}",
                        beforeSnapshots, afterSnapshots, added
                    );

                    // Use LoggingService as well (even if it's a 'warning', at least you see it)
                    await loggingService.LogWarningAsync(
                        $"SensorPollingService: processed IoT payload, added {added} snapshots (total now {afterSnapshots}).");
                }

                // 6) Wait until next poll
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(minutes), stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // normal on shutdown
                    break;
                }
            }
            catch (TaskCanceledException)
            {
                // normal on shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SensorPollingService loop");
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var loggingService = scope.ServiceProvider.GetRequiredService<LoggingService>();
                    await loggingService.LogWarningAsync("SensorPollingService error: " + ex.Message);
                }
                catch
                {
                    // ignore secondary logging failures
                }

                // avoid tight error loop
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        _logger.LogInformation("Sensor polling service stopped");
    }
}
