using LeakDetectionDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace LeakDetectionDashboard.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Zone> Zones => Set<Zone>();
    public DbSet<Pipe> Pipes => Set<Pipe>();
    public DbSet<Sensor> Sensors => Set<Sensor>();
    public DbSet<SensorSnapshot> SensorSnapshots => Set<SensorSnapshot>();
    public DbSet<Setting> Settings => Set<Setting>();
    public DbSet<LogEntry> LogEntries => Set<LogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Zone>()
            .HasMany(z => z.Pipes)
            .WithOne(p => p.Zone!)
            .HasForeignKey(p => p.ZoneId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Pipe>()
            .HasMany(p => p.Sensors)
            .WithOne(s => s.Pipe!)
            .HasForeignKey(s => s.PipeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Sensor>()
            .HasMany(s => s.Snapshots)
            .WithOne(ss => ss.Sensor!)
            .HasForeignKey(ss => ss.SensorId)
            .OnDelete(DeleteBehavior.Cascade);

        // ⚠️ IMPORTANT:
        // We REMOVE the seeding that was forcing a row with PollIntervalMinutes = 5.
        // The default (5) is now handled in SettingsService if the table is empty.
    }
}
