using LeakDetectionDashboard.Data;
using LeakDetectionDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace LeakDetectionDashboard.Services;

public class SettingsService
{
    private readonly AppDbContext _db;

    public SettingsService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<int> GetPollIntervalMinutesAsync(CancellationToken cancellationToken = default)
    {
        // Always read the actual value from the database
        var setting = await _db.Settings
            .AsNoTracking()
            .OrderBy(s => s.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (setting == null)
        {
            // First run: create a default row with 5 minutes
            setting = new Setting
            {
                PollIntervalMinutes = 5
            };

            _db.Settings.Add(setting);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return setting.PollIntervalMinutes;
    }

    public async Task UpdatePollIntervalMinutesAsync(int minutes, CancellationToken cancellationToken = default)
    {
        if (minutes < 1) minutes = 1;
        if (minutes > 60) minutes = 60;

        var setting = await _db.Settings
            .OrderBy(s => s.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (setting == null)
        {
            setting = new Setting
            {
                PollIntervalMinutes = minutes
            };
            _db.Settings.Add(setting);
        }
        else
        {
            setting.PollIntervalMinutes = minutes;
            _db.Settings.Update(setting);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
