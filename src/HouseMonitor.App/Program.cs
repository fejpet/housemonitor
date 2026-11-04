using Avalonia;
using HouseMonitor.Core.Clients;
using HouseMonitor.Core.Configuration;
using HouseMonitor.Core.Presentation;

namespace HouseMonitor.App;

internal static class Program
{
    [STAThread]
    public static async Task<int> Main(string[] args)
    {
        if (args.Contains("--console", StringComparer.OrdinalIgnoreCase))
        {
            var secretPath = SecretConfigurationLoader.ResolveSecretFilePath();
            var settings = await SecretConfigurationLoader.LoadFromFileAsync(secretPath);

            using var httpClient = new HttpClient();
            var client = new PowerStatusClient(httpClient, settings);
            var viewModel = new OverlayViewModel(client);
            await viewModel.RefreshOnceAsync();

            foreach (var line in viewModel.SummaryLines)
            {
                Console.WriteLine(line);
            }

            Console.WriteLine(viewModel.StatusMessage);
            return viewModel.HasError ? 1 : 0;
        }

        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
