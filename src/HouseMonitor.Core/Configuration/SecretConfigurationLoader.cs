using System.Text.Json;

namespace HouseMonitor.Core.Configuration;

/// <summary>
/// Root wrapper for the secret.json configuration file (REQ-009).
/// </summary>
public sealed class SecretConfiguration
{
    public ApiSettings? ApiSettings { get; init; }
}

/// <summary>
/// Settings required to authenticate and query the REST endpoint (REQ-002, REQ-008, REQ-009, REQ-016).
/// </summary>
public sealed class ApiSettings
{
    public string EndpointUrl { get; init; } = string.Empty;
    public string AuthHeaderName { get; init; } = "X-Auth-Key";
    public string AuthKey { get; init; } = string.Empty;
    public int RefreshIntervalSeconds { get; init; } = 10;
}

/// <summary>
/// Loads and validates API secret configuration from secret.json (REQ-009, REQ-010).
/// </summary>
public static class SecretConfigurationLoader
{
    private const string PlaceholderEndpointUrl = "<YOUR_ENDPOINT_URL>";
    private const string PlaceholderKey = "<YOUR_SECRET_API_KEY>";
    public const string DefaultSecretFileName = "secret.json";

    public static string ResolveSecretFilePath(string? startDirectory = null, string fileName = DefaultSecretFileName)
    {
        var current = new DirectoryInfo(startDirectory ?? Directory.GetCurrentDirectory());
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return Path.Combine(startDirectory ?? Directory.GetCurrentDirectory(), fileName);
    }

    public static async Task<ApiSettings> LoadFromFileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Secret configuration file was not found at '{filePath}'.", filePath);
        }

        var json = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
        return LoadFromJson(json);
    }

    public static ApiSettings LoadFromJson(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        var config = JsonSerializer.Deserialize<SecretConfiguration>(json);
        var settings = config?.ApiSettings
            ?? throw new InvalidOperationException("Missing 'ApiSettings' section in secret configuration.");

        if (string.IsNullOrWhiteSpace(settings.EndpointUrl) ||
            string.Equals(settings.EndpointUrl, PlaceholderEndpointUrl, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "A valid EndpointUrl must be configured in secret.json.");
        }

        if (string.IsNullOrWhiteSpace(settings.AuthHeaderName))
        {
            throw new InvalidOperationException("AuthHeaderName must be configured in ApiSettings.");
        }

        if (string.IsNullOrWhiteSpace(settings.AuthKey) ||
            string.Equals(settings.AuthKey, PlaceholderKey, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "A valid secret AuthKey must be configured in secret.json (placeholder value detected).");
        }

        if (settings.RefreshIntervalSeconds <= 0)
        {
            throw new InvalidOperationException(
                "RefreshIntervalSeconds must be a positive integer.");
        }

        return settings;
    }
}
