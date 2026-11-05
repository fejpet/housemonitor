using HouseMonitor.Core.Configuration;
using HouseMonitor.Core.Logging;
using HouseMonitor.Core.Models;

namespace HouseMonitor.Core.Clients;

/// <summary>
/// Abstraction for retrieving power telemetry from the remote REST endpoint (REQ-002).
/// </summary>
public interface IPowerStatusClient
{
    Task<PowerStatus> GetPowerStatusAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// HTTP REST client that queries the power telemetry endpoint using the configured X-Auth-Key header (REQ-002, REQ-008, REQ-021).
/// </summary>
public sealed class PowerStatusClient : IPowerStatusClient
{
    private readonly HttpClient _httpClient;
    private readonly ApiSettings _settings;
    private readonly IAppLogger _logger;

    public PowerStatusClient(
        HttpClient httpClient,
        ApiSettings settings,
        IAppLogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(settings);

        _httpClient = httpClient;
        _settings = settings;
        _logger = logger ?? NullAppLogger.Instance;
    }

    public async Task<PowerStatus> GetPowerStatusAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            nameof(PowerStatusClient),
            $"Sending HTTP GET request to '{_settings.EndpointUrl}' using header '{_settings.AuthHeaderName}'.");

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, _settings.EndpointUrl);
            request.Headers.Add(_settings.AuthHeaderName, _settings.AuthKey);

            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug(
                nameof(PowerStatusClient),
                $"Received HTTP response with status code {(int)response.StatusCode} ({response.StatusCode}).");

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogDebug(
                nameof(PowerStatusClient),
                $"Read JSON payload ({json.Length} characters). Deserializing PowerStatus.");

            var status = PowerStatus.FromJson(json);
            _logger.LogInfo(
                nameof(PowerStatusClient),
                $"Successfully retrieved power telemetry (Timestamp={status.Timestamp:O}, BatterySoc={status.Battery.Soc}%).");

            return status;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug(nameof(PowerStatusClient), "Telemetry HTTP request was cancelled.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                nameof(PowerStatusClient),
                $"Failed to retrieve or parse power telemetry from '{_settings.EndpointUrl}'.",
                ex);
            throw;
        }
    }
}
