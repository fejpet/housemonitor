using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using HouseMonitor.Core.Clients;
using HouseMonitor.Core.Configuration;
using HouseMonitor.Core.Presentation;
using HouseMonitor.Core.Services;

namespace HouseMonitor.App;

/// <summary>
/// Avalonia application bootstrapper that wires configuration, periodic polling, and the overlay window (REQ-015, REQ-016).
/// </summary>
public sealed class App : Application
{
    private CancellationTokenSource? _pollingCts;
    private HttpClient? _httpClient;

    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var secretPath = SecretConfigurationLoader.ResolveSecretFilePath();
            var settings = SecretConfigurationLoader.LoadFromFileAsync(secretPath).GetAwaiter().GetResult();

            _httpClient = new HttpClient();
            var client = new PowerStatusClient(_httpClient, settings);
            var viewModel = new OverlayViewModel(client);
            var pollingService = new PowerStatusPollingService(
                viewModel,
                TimeSpan.FromSeconds(settings.RefreshIntervalSeconds));

            _pollingCts = new CancellationTokenSource();
            _ = pollingService.RunAsync(_pollingCts.Token);

            desktop.MainWindow = new OverlayWindow(viewModel);
            desktop.Exit += (_, _) =>
            {
                _pollingCts?.Cancel();
                _pollingCts?.Dispose();
                _httpClient?.Dispose();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}

