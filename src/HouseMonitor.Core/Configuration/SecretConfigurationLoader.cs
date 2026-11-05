using System.Text.Json;
using HouseMonitor.Core.Logging;

namespace HouseMonitor.Core.Configuration;

/// <summary>
/// Root wrapper for the secret.json configuration file (REQ-009, REQ-022).
/// </summary>
public sealed class SecretConfiguration
{
    public ApiSettings? ApiSettings { get; init; }
    public LoggingSettings? LoggingSettings { get; init; }
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
/// Settings controlling log severity level and output sinks (REQ-021, REQ-022).
/// </summary>
public sealed class LoggingSettings
{
    public const string DefaultLogFilePath = "logs/housemonitor.log";

    public string Level { get; init; } = "INFO";
    public bool EnableConsole { get; init; } = true;
    public bool EnableFile { get; init; } = false;
    public string FilePath { get; init; } = DefaultLogFilePath;

    public LogLevel GetParsedLogLevel() => ParseLogLevel(Level);

    public static LogLevel ParseLogLevel(string? levelText)
    {
        if (string.IsNullOrWhiteSpace(levelText))
        {
            throw new InvalidOperationException("Log level must not be empty. Supported values: DEBUG, INFO, ERROR.");
        }

        return levelText.Trim().ToUpperInvariant() switch
        {
            "DEBUG" => LogLevel.Debug,
            "INFO" or "INFORMATION" => LogLevel.Info,
            "ERROR" => LogLevel.Error,
            _ => throw new InvalidOperationException(
                $"Unsupported log level '{levelText}'. Supported values: DEBUG, INFO, ERROR.")
        };
    }
}

/// <summary>
/// Loads and validates API secret and logging configuration from secret.json (REQ-009, REQ-010, REQ-021, REQ-022).
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
        var config = await LoadConfigurationFromFileAsync(filePath, logger: null, cancellationToken).ConfigureAwait(false);
        return config.ApiSettings!;
    }

    public static async Task<SecretConfiguration> LoadConfigurationFromFileAsync(
        string filePath,
        IAppLogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        var activeLogger = logger ?? NullAppLogger.Instance;
        activeLogger.LogDebug(nameof(SecretConfigurationLoader), $"Reading configuration file from '{filePath}'.");

        if (!File.Exists(filePath))
        {
            var ex = new FileNotFoundException($"Secret configuration file was not found at '{filePath}'.", filePath);
            activeLogger.LogError(nameof(SecretConfigurationLoader), $"Configuration file not found at '{filePath}'.", ex);
            throw ex;
        }

        var json = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
        return LoadConfigurationFromJson(json, activeLogger);
    }

    public static ApiSettings LoadFromJson(string json)
    {
        var config = LoadConfigurationFromJson(json);
        return config.ApiSettings!;
    }

    public static SecretConfiguration LoadConfigurationFromJson(string json, IAppLogger? logger = null)
    {
        var activeLogger = logger ?? NullAppLogger.Instance;

        try
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

            var loggingSettings = config?.LoggingSettings ?? new LoggingSettings();
            _ = loggingSettings.GetParsedLogLevel();

            if (loggingSettings.EnableFile && string.IsNullOrWhiteSpace(loggingSettings.FilePath))
            {
                throw new InvalidOperationException(
                    "A non-empty FilePath must be configured in LoggingSettings when EnableFile is true.");
            }

            activeLogger.LogInfo(
                nameof(SecretConfigurationLoader),
                $"Configuration validated successfully (EndpointUrl='{settings.EndpointUrl}', RefreshIntervalSeconds={settings.RefreshIntervalSeconds}, LogLevel={loggingSettings.GetParsedLogLevel()}).");

            return new SecretConfiguration
            {
                ApiSettings = settings,
                LoggingSettings = loggingSettings
            };
        }
        catch (Exception ex)
        {
            activeLogger.LogError(nameof(SecretConfigurationLoader), $"Configuration validation failed: {ex.Message}", ex);
            throw;
        }
    }
}
