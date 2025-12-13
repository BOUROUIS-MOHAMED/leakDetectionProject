/*using LeakDetectionDashboard.Data;
using LeakDetectionDashboard.Models;
using LeakDetectionDashboard.Models.Api;
using LeakDetectionDashboard.Models.ML;
using Microsoft.EntityFrameworkCore;

namespace LeakDetectionDashboard.Services;

public class LeakDetectionService
{
    private readonly AppDbContext _db;
    private readonly LeakDetectionModelService _modelService;
    private readonly LoggingService _loggingService;

    public LeakDetectionService(AppDbContext db, LeakDetectionModelService modelService, LoggingService loggingService)
    {
        _db = db;
        _modelService = modelService;
        _loggingService = loggingService;
    }

    public async Task ProcessReadingAsync(IoTReadingResponse payload, CancellationToken cancellationToken = default)
    {
        // 1. Upsert Zones, Pipes, Sensors
        await UpsertTopologyAsync(payload, cancellationToken);

        // Treat this IoT poll as "now" in the dashboard world
        var snapshotTime = DateTime.UtcNow;

        // 2. Build snapshots + predictions
        foreach (var sensorDto in payload.Sensors)
        {
            var sensorEntity = await _db.Sensors.FirstOrDefaultAsync(s => s.Id == sensorDto.Id, cancellationToken);
            if (sensorEntity == null)
                continue;

            var zoneId = payload.Pipes.First(p => p.Id == sensorDto.PipeId).ZoneId;
            var previousSensor = sensorDto.PreviousSensorId.HasValue
                ? payload.Sensors.FirstOrDefault(s => s.Id == sensorDto.PreviousSensorId.Value)
                : null;

            // Use snapshotTime (now) instead of historical unix timestamp
            var snapshot = BuildSnapshot(sensorDto, previousSensor, zoneId, snapshotTime);

            // 3. Build ML feature record
            var featureRecord = BuildFeatureRecord(snapshot);

            // 4. Predict leak probability
            var probability = _modelService.PredictLeakProbability(featureRecord);

            snapshot.LeakProbability = probability;
            snapshot.IsLeakPredicted = probability >= 0.5; // threshold you can tune
            snapshot.LeakSeverityPredicted = probability switch
            {
                < 0.2 => 0,
                < 0.4 => 1,
                < 0.6 => 1,
                < 0.8 => 2,
                _ => 3
            };
            snapshot.LeakTypePredicted = snapshot.IsLeakPredicted ? "continuous" : "none";

            _db.SensorSnapshots.Add(snapshot);
        }

        // Save newly added snapshots
        await _db.SaveChangesAsync(cancellationToken);

        // 5. Roll history: keep last 24 hours (for demo you can change this)
        var hoursToKeep = 24;
        var cutoff = DateTime.UtcNow.AddHours(-hoursToKeep);

        var oldSnapshots = await _db.SensorSnapshots
            .Where(s => s.Timestamp < cutoff)
            .ToListAsync(cancellationToken);

        if (oldSnapshots.Count > 0)
        {
            _db.SensorSnapshots.RemoveRange(oldSnapshots);
            await _db.SaveChangesAsync(cancellationToken);
        }

        await _loggingService.LogInfoAsync("Processed IoT reading " + payload.ReadingMetadata.ReadingId);
    }

    private async Task UpsertTopologyAsync(IoTReadingResponse payload, CancellationToken cancellationToken)
    {
        foreach (var zoneDto in payload.Zones)
        {
            var zone = await _db.Zones.FirstOrDefaultAsync(z => z.Id == zoneDto.Id, cancellationToken);
            if (zone == null)
            {
                zone = new Zone
                {
                    Id = zoneDto.Id,
                    Name = zoneDto.Name,
                    Status = zoneDto.Status
                };
                _db.Zones.Add(zone);
            }
            else
            {
                zone.Name = zoneDto.Name;
                zone.Status = zoneDto.Status;
            }
        }

        foreach (var pipeDto in payload.Pipes)
        {
            var pipe = await _db.Pipes.FirstOrDefaultAsync(p => p.Id == pipeDto.Id, cancellationToken);
            var installationDate = DateTimeOffset.FromUnixTimeSeconds(pipeDto.InstallationDate).UtcDateTime;

            if (pipe == null)
            {
                pipe = new Pipe
                {
                    Id = pipeDto.Id,
                    Name = pipeDto.Name,
                    ZoneId = pipeDto.ZoneId,
                    PreviousPipeId = pipeDto.PreviousPipeId,
                    Diameter = pipeDto.Diameter,
                    Length = pipeDto.Length,
                    Material = pipeDto.Material,
                    InstallationDate = installationDate,
                    ExpectedPressureDrop = pipeDto.ExpectedPressureDrop
                };
                _db.Pipes.Add(pipe);
            }
            else
            {
                pipe.Name = pipeDto.Name;
                pipe.ZoneId = pipeDto.ZoneId;
                pipe.PreviousPipeId = pipeDto.PreviousPipeId;
                pipe.Diameter = pipeDto.Diameter;
                pipe.Length = pipeDto.Length;
                pipe.Material = pipeDto.Material;
                pipe.InstallationDate = installationDate;
                pipe.ExpectedPressureDrop = pipeDto.ExpectedPressureDrop;
            }
        }

        foreach (var sensorDto in payload.Sensors)
        {
            var sensor = await _db.Sensors.FirstOrDefaultAsync(s => s.Id == sensorDto.Id, cancellationToken);
            var calibrationDate = DateTimeOffset.FromUnixTimeSeconds(sensorDto.LastCalibrationDate).UtcDateTime;

            if (sensor == null)
            {
                sensor = new Sensor
                {
                    Id = sensorDto.Id,
                    Name = sensorDto.Name,
                    PipeId = sensorDto.PipeId,
                    PreviousSensorId = sensorDto.PreviousSensorId,
                    Location = sensorDto.Location,
                    DistanceFromPreviousSensor = sensorDto.DistanceFromPreviousSensor,
                    Elevation = sensorDto.Elevation,
                    IsWaterTap = sensorDto.IsWaterTap,
                    ExpectedDailyUsage = sensorDto.ExpectedDailyUsage,
                    SensorStatus = sensorDto.SensorStatus,
                    LastCalibrationDate = calibrationDate
                };
                _db.Sensors.Add(sensor);
            }
            else
            {
                sensor.Name = sensorDto.Name;
                sensor.PipeId = sensorDto.PipeId;
                sensor.PreviousSensorId = sensorDto.PreviousSensorId;
                sensor.Location = sensorDto.Location;
                sensor.DistanceFromPreviousSensor = sensorDto.DistanceFromPreviousSensor;
                sensor.Elevation = sensorDto.Elevation;
                sensor.IsWaterTap = sensorDto.IsWaterTap;
                sensor.ExpectedDailyUsage = sensorDto.ExpectedDailyUsage;
                sensor.SensorStatus = sensorDto.SensorStatus;
                sensor.LastCalibrationDate = calibrationDate;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private SensorSnapshot BuildSnapshot(SensorDto sensorDto, SensorDto? previousSensorDto, int zoneId, DateTime snapshotTime)
    {
        var pressureSeries = sensorDto.Readings.PressureReadings;
        var flowSeries = sensorDto.Readings.FlowRateReadings;

        var lastPressure = pressureSeries.LastOrDefault()?.Value ?? sensorDto.Statistics.AveragePressure;
        var lastFlow = flowSeries.LastOrDefault()?.Value ?? sensorDto.Statistics.AverageFlowRate;

        double prevPressure = previousSensorDto != null
            ? (previousSensorDto.Readings.PressureReadings.LastOrDefault()?.Value ??
               previousSensorDto.Statistics.AveragePressure)
            : lastPressure;

        double waterUsageDiff = 0.0;
        if (previousSensorDto != null)
        {
            waterUsageDiff = previousSensorDto.Statistics.TotalFlowVolume - sensorDto.Statistics.TotalFlowVolume;
        }

        double pressureDropRate = ComputePressureDropRate(pressureSeries);

        // Time-based features – now, not historical timestamp from dataset
        var localTime = snapshotTime;
        int hour = localTime.Hour;
        int minute = localTime.Minute;
        int dayOfWeek = ((int)localTime.DayOfWeek + 6) % 7 + 1; // Monday=1 ... Sunday=7

        var (isWorking, isBreak, breakType, multiplier, minutesSinceBreakStart, occupancy) =
            ComputeScheduleFeatures(localTime);

        double baselinePressure = sensorDto.Statistics.AveragePressure;
        double baselineFlow = sensorDto.Statistics.AverageFlowRate;

        double pressureVsBaseline = lastPressure - baselinePressure;
        double flowVsBaseline = lastFlow - baselineFlow;

        return new SensorSnapshot
        {
            Timestamp = localTime,
            SensorId = sensorDto.Id,
            PipeId = sensorDto.PipeId,
            ZoneId = zoneId,

            PressureCurrent = lastPressure,
            PressurePreviousSensor = prevPressure,
            FlowRate = lastFlow,
            WaterUsageDiff = waterUsageDiff,
            PressureDropRate = pressureDropRate,

            Hour = hour,
            Minute = minute,
            DayOfWeek = dayOfWeek,
            IsWorkingHours = isWorking,
            IsBreakTime = isBreak,
            BreakType = breakType,
            ExpectedUsageMultiplier = multiplier,
            MinutesSinceBreakStart = minutesSinceBreakStart,
            OccupancyLevel = occupancy,
            PressureCurrentVsBaseline = pressureVsBaseline,
            FlowRateVsBaseline = flowVsBaseline
        };
    }

    private double ComputePressureDropRate(List<TimeSeriesReadingDto> series)
    {
        if (series.Count < 2) return 0.0;

        var first = series.First();
        var last = series.Last();
        var minutes = (last.Timestamp - first.Timestamp) / 60.0;
        if (minutes <= 0) return 0.0;

        return (last.Value - first.Value) / minutes;
    }

    private (bool isWorking, bool isBreak, string breakType, double multiplier, int minutesSinceBreakStart, double occupancy)
        ComputeScheduleFeatures(DateTime time)
    {
        // Based on your Monday-Saturday schedule
        int dayOfWeek = ((int)time.DayOfWeek + 6) % 7 + 1; // Monday=1..Sunday=7

        bool isWorkingDay = dayOfWeek is >= 1 and <= 6;
        var hour = time.Hour;
        var minute = time.Minute;
        var totalMinutes = hour * 60 + minute;

        bool isWorkingHours = isWorkingDay && totalMinutes >= 7 * 60 && totalMinutes < 19 * 60;

        bool isBreak = false;
        string breakType = "none";
        double multiplier = 1.0;
        int minutesSinceBreakStart = 0;
        double occupancy = 0.3;

        if (!isWorkingDay)
        {
            // Sunday - minimal activity
            isWorkingHours = false;
            multiplier = 0.1;
            occupancy = 0.1;
            return (isWorkingHours, isBreak, breakType, multiplier, minutesSinceBreakStart, occupancy);
        }

        // Define helper for ranges
        bool InRange(int startH, int startM, int endH, int endM)
        {
            var s = startH * 60 + startM;
            var e = endH * 60 + endM;
            return totalMinutes >= s && totalMinutes < e;
        }

        // Breaks
        if (InRange(10, 0, 10, 10) || InRange(11, 50, 12, 0) ||
            InRange(15, 20, 15, 30) || InRange(17, 0, 17, 10))
        {
            isBreak = true;
            breakType = "short";
            multiplier = 2.8;
            minutesSinceBreakStart = totalMinutes % 10;
            occupancy = 0.8;
        }
        else if (InRange(13, 30, 14, 0))
        {
            isBreak = true;
            breakType = "lunch";
            multiplier = 4.0;
            minutesSinceBreakStart = totalMinutes - (13 * 60 + 30);
            occupancy = 0.95;
        }
        else if (isWorkingHours)
        {
            // Working hours but not break
            breakType = "none";
            multiplier = 1.3;
            occupancy = 0.35;
        }
        else
        {
            // After hours
            multiplier = 0.2;
            occupancy = 0.1;
        }

        return (isWorkingHours, isBreak, breakType, multiplier, minutesSinceBreakStart, occupancy);
    }

    private LeakTrainingRecord BuildFeatureRecord(SensorSnapshot snapshot)
    {
        return new LeakTrainingRecord
        {
            Timestamp = snapshot.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
            SensorId = snapshot.SensorId,
            PipeId = snapshot.PipeId,
            ZoneId = snapshot.ZoneId,
            PressureCurrent = (float)snapshot.PressureCurrent,
            PressurePreviousSensor = (float)snapshot.PressurePreviousSensor,
            FlowRate = (float)snapshot.FlowRate,
            WaterUsageDiff = (float)snapshot.WaterUsageDiff,
            PressureDropRate = (float)snapshot.PressureDropRate,
            Hour = snapshot.Hour,
            Minute = snapshot.Minute,
            DayOfWeek = snapshot.DayOfWeek,
            IsWorkingHours = snapshot.IsWorkingHours ? 1f : 0f,
            IsBreakTime = snapshot.IsBreakTime ? 1f : 0f,
            BreakType = snapshot.BreakType,
            ExpectedUsageMultiplier = (float)snapshot.ExpectedUsageMultiplier,
            MinutesSinceBreakStart = snapshot.MinutesSinceBreakStart,
            OccupancyLevel = (float)snapshot.OccupancyLevel,
            PressureCurrentVsBaseline = (float)snapshot.PressureCurrentVsBaseline,
            FlowRateVsBaseline = (float)snapshot.FlowRateVsBaseline,
            IsLeak = false, // not used at prediction time
            LeakSeverity = 0,
            LeakType = "none"
        };
    }

    public async Task<List<LeakPredictionResult>> GetCurrentPipeRiskAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var cutoff = now.AddHours(-1); // look at last hour snapshots

        var snapshots = await _db.SensorSnapshots
            .Where(s => s.Timestamp >= cutoff)
            .ToListAsync(cancellationToken);

        var pipes = await _db.Pipes.Include(p => p.Zone!).ToListAsync(cancellationToken);

        var grouped = snapshots
            .GroupBy(s => s.PipeId)
            .Select(g =>
            {
                var pipe = pipes.First(p => p.Id == g.Key);
                var prob = g.Max(x => x.LeakProbability);
                return new LeakPredictionResult
                {
                    PipeId = pipe.Id,
                    PipeName = pipe.Name,
                    ZoneId = pipe.ZoneId,
                    ZoneName = pipe.Zone?.Name ?? "",
                    LeakProbability = prob
                };
            })
            .ToList();

        return grouped;
    }
}
*/
using LeakDetectionDashboard.Data;
using LeakDetectionDashboard.Models;
using LeakDetectionDashboard.Models.Api;
using LeakDetectionDashboard.Models.ML;
using Microsoft.EntityFrameworkCore;

namespace LeakDetectionDashboard.Services;

public class LeakDetectionService
{
    private readonly AppDbContext _db;
    private readonly LeakDetectionModelService _modelService;
    private readonly LoggingService _loggingService;

    public LeakDetectionService(AppDbContext db, LeakDetectionModelService modelService, LoggingService loggingService)
    {
        _db = db;
        _modelService = modelService;
        _loggingService = loggingService;
    }

    public async Task ProcessReadingAsync(IoTReadingResponse payload, CancellationToken cancellationToken = default)
    {
        // 1. Upsert Zones, Pipes, Sensors
        await UpsertTopologyAsync(payload, cancellationToken);

        // Treat this IoT poll as "now" in the dashboard world
        var snapshotTime = DateTime.UtcNow;

        // 2. Build snapshots + predictions
        foreach (var sensorDto in payload.Sensors)
        {
            var sensorEntity = await _db.Sensors.FirstOrDefaultAsync(s => s.Id == sensorDto.Id, cancellationToken);
            if (sensorEntity == null)
                continue;

            var zoneId = payload.Pipes.First(p => p.Id == sensorDto.PipeId).ZoneId;
            var previousSensor = sensorDto.PreviousSensorId.HasValue
                ? payload.Sensors.FirstOrDefault(s => s.Id == sensorDto.PreviousSensorId.Value)
                : null;

            // Use snapshotTime (now) instead of historical unix timestamp
            var snapshot = BuildSnapshot(sensorDto, previousSensor, zoneId, snapshotTime);

            // 3. Build ML feature record
            var featureRecord = BuildFeatureRecord(snapshot);

            // 4. Predict leak probability
            var probability = _modelService.PredictLeakProbability(featureRecord);

            snapshot.LeakProbability = probability;
            snapshot.IsLeakPredicted = probability >= 0.5; // threshold you can tune
            snapshot.LeakSeverityPredicted = probability switch
            {
                < 0.2 => 0,
                < 0.4 => 1,
                < 0.6 => 1,
                < 0.8 => 2,
                _ => 3
            };
            snapshot.LeakTypePredicted = snapshot.IsLeakPredicted ? "continuous" : "none";

            _db.SensorSnapshots.Add(snapshot);
        }

        // Save newly added snapshots
        await _db.SaveChangesAsync(cancellationToken);

        // 5. Roll history: keep last 24 hours (for demo you can change this)
        var hoursToKeep = 24;
        var cutoff = DateTime.UtcNow.AddHours(-hoursToKeep);

        var oldSnapshots = await _db.SensorSnapshots
            .Where(s => s.Timestamp < cutoff)
            .ToListAsync(cancellationToken);

        if (oldSnapshots.Count > 0)
        {
            _db.SensorSnapshots.RemoveRange(oldSnapshots);
            await _db.SaveChangesAsync(cancellationToken);
        }

        await _loggingService.LogInfoAsync("Processed IoT reading " + payload.ReadingMetadata.ReadingId);
    }

    private async Task UpsertTopologyAsync(IoTReadingResponse payload, CancellationToken cancellationToken)
    {
        foreach (var zoneDto in payload.Zones)
        {
            var zone = await _db.Zones.FirstOrDefaultAsync(z => z.Id == zoneDto.Id, cancellationToken);
            if (zone == null)
            {
                zone = new Zone
                {
                    Id = zoneDto.Id,
                    Name = zoneDto.Name,
                    Status = zoneDto.Status
                };
                _db.Zones.Add(zone);
            }
            else
            {
                zone.Name = zoneDto.Name;
                zone.Status = zoneDto.Status;
            }
        }

        foreach (var pipeDto in payload.Pipes)
        {
            var pipe = await _db.Pipes.FirstOrDefaultAsync(p => p.Id == pipeDto.Id, cancellationToken);
            var installationDate = DateTimeOffset.FromUnixTimeSeconds(pipeDto.InstallationDate).UtcDateTime;

            if (pipe == null)
            {
                pipe = new Pipe
                {
                    Id = pipeDto.Id,
                    Name = pipeDto.Name,
                    ZoneId = pipeDto.ZoneId,
                    PreviousPipeId = pipeDto.PreviousPipeId,
                    Diameter = pipeDto.Diameter,
                    Length = pipeDto.Length,
                    Material = pipeDto.Material,
                    InstallationDate = installationDate,
                    ExpectedPressureDrop = pipeDto.ExpectedPressureDrop
                };
                _db.Pipes.Add(pipe);
            }
            else
            {
                pipe.Name = pipeDto.Name;
                pipe.ZoneId = pipeDto.ZoneId;
                pipe.PreviousPipeId = pipeDto.PreviousPipeId;
                pipe.Diameter = pipeDto.Diameter;
                pipe.Length = pipeDto.Length;
                pipe.Material = pipeDto.Material;
                pipe.InstallationDate = installationDate;
                pipe.ExpectedPressureDrop = pipeDto.ExpectedPressureDrop;
            }
        }

        foreach (var sensorDto in payload.Sensors)
        {
            var sensor = await _db.Sensors.FirstOrDefaultAsync(s => s.Id == sensorDto.Id, cancellationToken);
            var calibrationDate = DateTimeOffset.FromUnixTimeSeconds(sensorDto.LastCalibrationDate).UtcDateTime;

            if (sensor == null)
            {
                sensor = new Sensor
                {
                    Id = sensorDto.Id,
                    Name = sensorDto.Name,
                    PipeId = sensorDto.PipeId,
                    PreviousSensorId = sensorDto.PreviousSensorId,
                    Location = sensorDto.Location,
                    DistanceFromPreviousSensor = sensorDto.DistanceFromPreviousSensor,
                    Elevation = sensorDto.Elevation,
                    IsWaterTap = sensorDto.IsWaterTap,
                    ExpectedDailyUsage = sensorDto.ExpectedDailyUsage,
                    SensorStatus = sensorDto.SensorStatus,
                    LastCalibrationDate = calibrationDate
                };
                _db.Sensors.Add(sensor);
            }
            else
            {
                sensor.Name = sensorDto.Name;
                sensor.PipeId = sensorDto.PipeId;
                sensor.PreviousSensorId = sensorDto.PreviousSensorId;
                sensor.Location = sensorDto.Location;
                sensor.DistanceFromPreviousSensor = sensorDto.DistanceFromPreviousSensor;
                sensor.Elevation = sensorDto.Elevation;
                sensor.IsWaterTap = sensorDto.IsWaterTap;
                sensor.ExpectedDailyUsage = sensorDto.ExpectedDailyUsage;
                sensor.SensorStatus = sensorDto.SensorStatus;
                sensor.LastCalibrationDate = calibrationDate;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private SensorSnapshot BuildSnapshot(SensorDto sensorDto, SensorDto? previousSensorDto, int zoneId, DateTime snapshotTime)
    {
        var pressureSeries = sensorDto.Readings.PressureReadings;
        var flowSeries = sensorDto.Readings.FlowRateReadings;

        var lastPressure = pressureSeries.LastOrDefault()?.Value ?? sensorDto.Statistics.AveragePressure;
        var lastFlow = flowSeries.LastOrDefault()?.Value ?? sensorDto.Statistics.AverageFlowRate;

        double prevPressure = previousSensorDto != null
            ? (previousSensorDto.Readings.PressureReadings.LastOrDefault()?.Value ??
               previousSensorDto.Statistics.AveragePressure)
            : lastPressure;

        double waterUsageDiff = 0.0;
        if (previousSensorDto != null)
        {
            waterUsageDiff = previousSensorDto.Statistics.TotalFlowVolume - sensorDto.Statistics.TotalFlowVolume;
        }

        double pressureDropRate = ComputePressureDropRate(pressureSeries);

        // Time-based features – now, not historical timestamp from dataset
        var localTime = snapshotTime;
        int hour = localTime.Hour;
        int minute = localTime.Minute;
        int dayOfWeek = ((int)localTime.DayOfWeek + 6) % 7 + 1; // Monday=1 ... Sunday=7

        var (isWorking, isBreak, breakType, multiplier, minutesSinceBreakStart, occupancy) =
            ComputeScheduleFeatures(localTime);

        double baselinePressure = sensorDto.Statistics.AveragePressure;
        double baselineFlow = sensorDto.Statistics.AverageFlowRate;

        double pressureVsBaseline = lastPressure - baselinePressure;
        double flowVsBaseline = lastFlow - baselineFlow;

        return new SensorSnapshot
        {
            Timestamp = localTime,
            SensorId = sensorDto.Id,
            PipeId = sensorDto.PipeId,
            ZoneId = zoneId,

            PressureCurrent = lastPressure,
            PressurePreviousSensor = prevPressure,
            FlowRate = lastFlow,
            WaterUsageDiff = waterUsageDiff,
            PressureDropRate = pressureDropRate,

            Hour = hour,
            Minute = minute,
            DayOfWeek = dayOfWeek,
            IsWorkingHours = isWorking,
            IsBreakTime = isBreak,
            BreakType = breakType,
            ExpectedUsageMultiplier = multiplier,
            MinutesSinceBreakStart = minutesSinceBreakStart,
            OccupancyLevel = occupancy,
            PressureCurrentVsBaseline = pressureVsBaseline,
            FlowRateVsBaseline = flowVsBaseline
        };
    }

    private double ComputePressureDropRate(List<TimeSeriesReadingDto> series)
    {
        if (series.Count < 2) return 0.0;

        var first = series.First();
        var last = series.Last();
        var minutes = (last.Timestamp - first.Timestamp) / 60.0;
        if (minutes <= 0) return 0.0;

        return (last.Value - first.Value) / minutes;
    }

    private (bool isWorking, bool isBreak, string breakType, double multiplier, int minutesSinceBreakStart, double occupancy)
        ComputeScheduleFeatures(DateTime time)
    {
        // Based on your Monday-Saturday schedule
        int dayOfWeek = ((int)time.DayOfWeek + 6) % 7 + 1; // Monday=1..Sunday=7

        bool isWorkingDay = dayOfWeek is >= 1 and <= 6;
        var hour = time.Hour;
        var minute = time.Minute;
        var totalMinutes = hour * 60 + minute;

        bool isWorkingHours = isWorkingDay && totalMinutes >= 7 * 60 && totalMinutes < 19 * 60;

        bool isBreak = false;
        string breakType = "none";
        double multiplier = 1.0;
        int minutesSinceBreakStart = 0;
        double occupancy = 0.3;

        if (!isWorkingDay)
        {
            // Sunday - minimal activity
            isWorkingHours = false;
            multiplier = 0.1;
            occupancy = 0.1;
            return (isWorkingHours, isBreak, breakType, multiplier, minutesSinceBreakStart, occupancy);
        }

        // Define helper for ranges
        bool InRange(int startH, int startM, int endH, int endM)
        {
            var s = startH * 60 + startM;
            var e = endH * 60 + endM;
            return totalMinutes >= s && totalMinutes < e;
        }

        // Breaks
        if (InRange(10, 0, 10, 10) || InRange(11, 50, 12, 0) ||
            InRange(15, 20, 15, 30) || InRange(17, 0, 17, 10))
        {
            isBreak = true;
            breakType = "short";
            multiplier = 2.8;
            minutesSinceBreakStart = totalMinutes % 10;
            occupancy = 0.8;
        }
        else if (InRange(13, 30, 14, 0))
        {
            isBreak = true;
            breakType = "lunch";
            multiplier = 4.0;
            minutesSinceBreakStart = totalMinutes - (13 * 60 + 30);
            occupancy = 0.95;
        }
        else if (isWorkingHours)
        {
            // Working hours but not break
            breakType = "none";
            multiplier = 1.3;
            occupancy = 0.35;
        }
        else
        {
            // After hours
            multiplier = 0.2;
            occupancy = 0.1;
        }

        return (isWorkingHours, isBreak, breakType, multiplier, minutesSinceBreakStart, occupancy);
    }

    private LeakTrainingRecord BuildFeatureRecord(SensorSnapshot snapshot)
    {
        return new LeakTrainingRecord
        {
            Timestamp = snapshot.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
            SensorId = snapshot.SensorId,
            PipeId = snapshot.PipeId,
            ZoneId = snapshot.ZoneId,
            PressureCurrent = (float)snapshot.PressureCurrent,
            PressurePreviousSensor = (float)snapshot.PressurePreviousSensor,
            FlowRate = (float)snapshot.FlowRate,
            WaterUsageDiff = (float)snapshot.WaterUsageDiff,
            PressureDropRate = (float)snapshot.PressureDropRate,
            Hour = snapshot.Hour,
            Minute = snapshot.Minute,
            DayOfWeek = snapshot.DayOfWeek,
            IsWorkingHours = snapshot.IsWorkingHours ? 1f : 0f,
            IsBreakTime = snapshot.IsBreakTime ? 1f : 0f,
            BreakType = snapshot.BreakType,
            ExpectedUsageMultiplier = (float)snapshot.ExpectedUsageMultiplier,
            MinutesSinceBreakStart = snapshot.MinutesSinceBreakStart,
            OccupancyLevel = (float)snapshot.OccupancyLevel,
            PressureCurrentVsBaseline = (float)snapshot.PressureCurrentVsBaseline,
            FlowRateVsBaseline = (float)snapshot.FlowRateVsBaseline,
            IsLeak = false, // not used at prediction time
            LeakSeverity = 0,
            LeakType = "none"
        };
    }

    public async Task<List<LeakPredictionResult>> GetCurrentPipeRiskAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var cutoff = now.AddHours(-1); // consider snapshots from the last hour

        var snapshots = await _db.SensorSnapshots
            .Where(s => s.Timestamp >= cutoff)
            .ToListAsync(cancellationToken);

        var pipes = await _db.Pipes.Include(p => p.Zone!).ToListAsync(cancellationToken);

        // For each pipe, take the LATEST snapshot (most recent reading),
        // not the maximum probability across the whole hour.
        var grouped = snapshots
            .GroupBy(s => s.PipeId)
            .Select(g =>
            {
                var pipe = pipes.First(p => p.Id == g.Key);
                var latest = g.OrderByDescending(x => x.Timestamp).First();

                return new LeakPredictionResult
                {
                    PipeId = pipe.Id,
                    PipeName = pipe.Name,
                    ZoneId = pipe.ZoneId,
                    ZoneName = pipe.Zone?.Name ?? "",
                    LeakProbability = latest.LeakProbability
                };
            })
            .ToList();

        return grouped;
    }
}
