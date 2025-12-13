using System.Text.Json;
using LeakDetectionDashboard.Models.Api;

namespace LeakDetectionDashboard.Services;

public class FakeIotClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _readingsEndpoint;
    private readonly ILogger<FakeIotClient> _logger;

    public FakeIotClient(HttpClient httpClient, IConfiguration configuration, ILogger<FakeIotClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        // Base URL of your FastAPI fake IoT backend
        _baseUrl = configuration["FakeIot:BaseUrl"] ?? "http://localhost:8000";

        // Endpoint path for readings
        // For your backend, this should be "/api/v1/readings"
        _readingsEndpoint = configuration["FakeIot:ReadingsEndpoint"] ?? "/api/v1/readings";
    }

    public async Task<IoTReadingResponse?> GetReadingsAsync(
        int historyMinutes,
        CancellationToken cancellationToken = default)
    {
        var baseUrlNormalized = _baseUrl.TrimEnd('/');
        var endpointNormalized = _readingsEndpoint.StartsWith("/")
            ? _readingsEndpoint
            : "/" + _readingsEndpoint;

        // This matches your FastAPI route and the call you tested manually:
        // GET /api/v1/readings?window_minutes=5&data_points_per_sensor=5
        var url =
            $"{baseUrlNormalized}{endpointNormalized}" +
            $"?window_minutes={historyMinutes}&data_points_per_sensor=5";

        _logger.LogInformation("Requesting IoT readings from {Url}", url);

        try
        {
            // make the HTTP call and dispose response automatically
            using var response = await _httpClient.GetAsync(url, cancellationToken);

            _logger.LogInformation(
                "FakeIoT backend responded with status code {StatusCode}",
                response.StatusCode
            );

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "FakeIoT backend returned non-success status code {StatusCode}",
                    response.StatusCode
                );
                return null;
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var result =
                await JsonSerializer.DeserializeAsync<IoTReadingResponse>(stream, options, cancellationToken);

            if (result == null)
            {
                _logger.LogWarning("Deserialized IoTReadingResponse is null.");
            }
            else
            {
                _logger.LogInformation(
                    "Deserialized IoTReadingResponse: {Zones} zones, {Pipes} pipes, {Sensors} sensors",
                    result.Zones?.Count ?? 0,
                    result.Pipes?.Count ?? 0,
                    result.Sensors?.Count ?? 0
                );
            }

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while calling FakeIoT backend.");
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error when reading FakeIoT response.");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in FakeIotClient.GetReadingsAsync.");
            return null;
        }
    }
}
