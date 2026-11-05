using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using HouseMonitor.Core.Clients;
using HouseMonitor.Core.Configuration;
using HouseMonitor.Core.Logging;
using HouseMonitor.Core.Presentation;
using HouseMonitor.Core.Services;

namespace HouseMonitor.App;

/// <summary>
/// Avalonia application bootstrapper that wires configuration, logging, periodic polling, and the overlay window (REQ-015, REQ-016, REQ-021, REQ-022).
/// </summary>
public sealed class App : Application
{
    private CancellationTokenSource? _pollingCts;
    private HttpClient? _httpClient;
    private AppLogger? _logger;

    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var cliOptions = Program.CliOptions;
            var secretPath = !string.IsNullOrWhiteSpace(cliOptions.SecretFilePathOverride)
                ? cliOptions.SecretFilePathOverride
                : SecretConfigurationLoader.ResolveSecretFilePath();

            var config = SecretConfigurationLoader.LoadConfigurationFromFileAsync(secretPath).GetAwaiter().GetResult();
            var settings = config.ApiSettings!;
            var loggingSettings = cliOptions.ApplyOverrides(config.LoggingSettings);

            _logger = AppLogger.Create(loggingSettings);
            _logger.LogInfo(nameof(App), $"Starting HouseMonitor desktop overlay (LogLevel={_logger.MinimumLevel}, Console={loggingSettings.EnableConsole}, File={loggingSettings.EnableFile}).");

            _httpClient = new HttpClient();
            var client = new PowerStatusClient(_httpClient, settings, _logger);
            var viewModel = new OverlayViewModel(client, _logger);
            var pollingService = new PowerStatusPollingService(
                viewModel,
                TimeSpan.FromSeconds(settings.RefreshIntervalSeconds),
                logger: _logger);

            _pollingCts = new CancellationTokenSource();
            _ = pollingService.RunAsync(_pollingCts.Token);

            desktop.MainWindow = new OverlayWindow(viewModel);
            desktop.Exit += (_, _) =>
            {
                _logger?.LogInfo(nameof(App), "Shutting down HouseMonitor desktop overlay.");
                _pollingCts?.Cancel();
                _pollingCts?.Dispose();
                _httpClient?.Dispose();
                _logger?.Dispose();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
