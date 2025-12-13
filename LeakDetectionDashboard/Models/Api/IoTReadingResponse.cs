using System.Text.Json.Serialization;

namespace LeakDetectionDashboard.Models.Api;

public class IoTReadingResponse
{
    [JsonPropertyName("readingMetadata")]
    public ReadingMetadata ReadingMetadata { get; set; } = new();

    [JsonPropertyName("zones")]
    public List<ZoneDto> Zones { get; set; } = new();

    [JsonPropertyName("pipes")]
    public List<PipeDto> Pipes { get; set; } = new();

    [JsonPropertyName("sensors")]
    public List<SensorDto> Sensors { get; set; } = new();

    [JsonPropertyName("systemHealth")]
    public SystemHealthDto SystemHealth { get; set; } = new();
}

public class ReadingMetadata
{
    [JsonPropertyName("readingId")]
    public string ReadingId { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    [JsonPropertyName("intervalMinutes")]
    public int IntervalMinutes { get; set; }

    [JsonPropertyName("dataPointsPerSensor")]
    public int DataPointsPerSensor { get; set; }
}

public class ZoneDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("totalSensors")]
    public int TotalSensors { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "normal";
}

public class PipeDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("zoneId")]
    public int ZoneId { get; set; }

    [JsonPropertyName("previousPipeId")]
    public int? PreviousPipeId { get; set; }

    [JsonPropertyName("diameter")]
    public double Diameter { get; set; }

    [JsonPropertyName("length")]
    public double Length { get; set; }

    [JsonPropertyName("material")]
    public string Material { get; set; } = string.Empty;

    [JsonPropertyName("installationDate")]
    public long InstallationDate { get; set; }

    [JsonPropertyName("expectedPressureDrop")]
    public double ExpectedPressureDrop { get; set; }
}

public class SensorDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("pipeId")]
    public int PipeId { get; set; }

    [JsonPropertyName("previousSensorId")]
    public int? PreviousSensorId { get; set; }

    [JsonPropertyName("location")]
    public string Location { get; set; } = string.Empty;

    [JsonPropertyName("distanceFromPreviousSensor")]
    public double DistanceFromPreviousSensor { get; set; }

    [JsonPropertyName("elevation")]
    public double Elevation { get; set; }

    [JsonPropertyName("isWaterTap")]
    public bool IsWaterTap { get; set; }

    [JsonPropertyName("expectedDailyUsage")]
    public double ExpectedDailyUsage { get; set; }

    [JsonPropertyName("sensorStatus")]
    public string SensorStatus { get; set; } = "active";

    [JsonPropertyName("lastCalibrationDate")]
    public long LastCalibrationDate { get; set; }

    [JsonPropertyName("readings")]
    public SensorReadingsDto Readings { get; set; } = new();

    [JsonPropertyName("statistics")]
    public SensorStatisticsDto Statistics { get; set; } = new();
}

public class SensorReadingsDto
{
    [JsonPropertyName("startTimestamp")]
    public long StartTimestamp { get; set; }

    [JsonPropertyName("endTimestamp")]
    public long EndTimestamp { get; set; }

    [JsonPropertyName("intervalSeconds")]
    public int IntervalSeconds { get; set; }

    [JsonPropertyName("pressureReadings")]
    public List<TimeSeriesReadingDto> PressureReadings { get; set; } = new();

    [JsonPropertyName("flowRateReadings")]
    public List<TimeSeriesReadingDto> FlowRateReadings { get; set; } = new();

    [JsonPropertyName("temperatureReadings")]
    public List<TimeSeriesReadingDto> TemperatureReadings { get; set; } = new();
}

public class TimeSeriesReadingDto
{
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    [JsonPropertyName("value")]
    public double Value { get; set; }

    [JsonPropertyName("quality")]
    public string? Quality { get; set; }
}

public class SensorStatisticsDto
{
    [JsonPropertyName("totalWaterUsage")]
    public double TotalWaterUsage { get; set; }

    [JsonPropertyName("averagePressure")]
    public double AveragePressure { get; set; }

    [JsonPropertyName("minPressure")]
    public double MinPressure { get; set; }

    [JsonPropertyName("maxPressure")]
    public double MaxPressure { get; set; }

    [JsonPropertyName("pressureVariance")]
    public double PressureVariance { get; set; }

    [JsonPropertyName("averageFlowRate")]
    public double AverageFlowRate { get; set; }

    [JsonPropertyName("totalFlowVolume")]
    public double TotalFlowVolume { get; set; }

    [JsonPropertyName("pressureDropRate")]
    public double PressureDropRate { get; set; }

    [JsonPropertyName("anomalyScore")]
    public double AnomalyScore { get; set; }
}

public class SystemHealthDto
{
    [JsonPropertyName("totalSensorsActive")]
    public int TotalSensorsActive { get; set; }

    [JsonPropertyName("totalSensorsInactive")]
    public int TotalSensorsInactive { get; set; }

    [JsonPropertyName("dataQualityScore")]
    public double DataQualityScore { get; set; }

    [JsonPropertyName("lastSystemCheckTimestamp")]
    public long LastSystemCheckTimestamp { get; set; }

    [JsonPropertyName("networkLatency")]
    public int NetworkLatency { get; set; }
}
