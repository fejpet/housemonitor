using HouseMonitor.Core.Configuration;
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
/// HTTP REST client that queries the power telemetry endpoint using the configured X-Auth-Key header (REQ-002, REQ-008).
/// </summary>
public sealed class PowerStatusClient : IPowerStatusClient
{
    private readonly HttpClient _httpClient;
    private readonly ApiSettings _settings;

    public PowerStatusClient(HttpClient httpClient, ApiSettings settings)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(settings);

        _httpClient = httpClient;
        _settings = settings;
    }

    public async Task<PowerStatus> GetPowerStatusAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, _settings.EndpointUrl);
        request.Headers.Add(_settings.AuthHeaderName, _settings.AuthKey);

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return PowerStatus.FromJson(json);
    }
}

