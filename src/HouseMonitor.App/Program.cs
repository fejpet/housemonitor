using System.Runtime.InteropServices;
using Avalonia;
using HouseMonitor.Core.Clients;
using HouseMonitor.Core.Configuration;
using HouseMonitor.Core.Logging;
using HouseMonitor.Core.Presentation;

namespace HouseMonitor.App;

internal static class Program
{
    internal static CommandLineOptions CliOptions { get; private set; } = new();

    [STAThread]
    public static async Task<int> Main(string[] args)
    {
        CliOptions = CommandLineOptions.Parse(args);

        if (CliOptions.ShowHelp)
        {
            Console.WriteLine(CommandLineOptions.GetHelpText());
            return 0;
        }

        if (CliOptions.ConsoleMode)
        {
            var secretPath = !string.IsNullOrWhiteSpace(CliOptions.SecretFilePathOverride)
                ? CliOptions.SecretFilePathOverride
                : SecretConfigurationLoader.ResolveSecretFilePath();

            var config = await SecretConfigurationLoader.LoadConfigurationFromFileAsync(secretPath);
            var loggingSettings = CliOptions.ApplyOverrides(config.LoggingSettings);
            using var logger = AppLogger.Create(loggingSettings);

            logger.LogInfo(nameof(Program), $"Starting HouseMonitor in console mode using configuration '{secretPath}'.");

            using var httpClient = new HttpClient();
            var client = new PowerStatusClient(httpClient, config.ApiSettings!, logger);
            var viewModel = new OverlayViewModel(client, logger);
            await viewModel.RefreshOnceAsync();

            foreach (var line in viewModel.SummaryLines)
            {
                Console.WriteLine(line);
            }

            Console.WriteLine(viewModel.StatusMessage);
            logger.LogInfo(nameof(Program), $"Console mode execution completed (HasError={viewModel.HasError}).");
            return viewModel.HasError ? 1 : 0;
        }

        RegisterLinuxX11FallbackResolver();
        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .With(new X11PlatformOptions
            {
                EnableSessionManagement = false
            })
            .WithInterFont()
            .LogToTrace();

    private static void RegisterLinuxX11FallbackResolver()
    {
        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        var x11Assembly = typeof(X11PlatformOptions).Assembly;
        NativeLibrary.SetDllImportResolver(x11Assembly, (libraryName, assembly, searchPath) =>
        {
            if (NativeLibrary.TryLoad(libraryName, assembly, searchPath, out var handle))
            {
                return handle;
            }

            if (libraryName is "libICE.so.6" or "libSM.so.6")
            {
                var fallbackPath = Path.Combine(AppContext.BaseDirectory, libraryName);
                if (NativeLibrary.TryLoad(fallbackPath, out var fallbackHandle))
                {
                    return fallbackHandle;
                }
            }

            return IntPtr.Zero;
        });
    }
}
