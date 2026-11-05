using System.Runtime.InteropServices;
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
