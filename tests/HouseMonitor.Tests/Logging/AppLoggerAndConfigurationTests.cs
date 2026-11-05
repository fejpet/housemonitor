using System.Net;
using System.Text;
using HouseMonitor.Core.Clients;
using HouseMonitor.Core.Configuration;
using HouseMonitor.Core.Logging;
using HouseMonitor.Core.Models;
using HouseMonitor.Core.Presentation;
using HouseMonitor.Core.Services;

namespace HouseMonitor.Tests.Logging;

public class AppLoggerAndConfigurationTests
{
    private const string ValidSampleResponseJson = """
        {
          "battery": { "soc": 65.0, "state": "discharging" },
          "generator": { "state": "stopped", "today": 0 },
          "solar": { "production": 0.0, "today": 17.98 },
          "house": { "consumption": 415.0, "today_consumtion": 13.38 },
          "timestamp": "2026-11-04T22:20:35.014353+01:00"
        }
        """;

    [Fact]
    public void AppLogger_DefaultInfoLevel_FiltersOutDebugAndWritesInfoAndError()
    {
        // Arrange (REQ-021)
        var memorySink = new InMemoryLogSink();
        using var logger = new AppLogger(LogLevel.Info, [memorySink]);

        // Act
        logger.LogDebug("TestCategory", "Detailed debug trace");
        logger.LogInfo("TestCategory", "Major lifecycle event");
        logger.LogError("TestCategory", "Operation failed", new InvalidOperationException("Boom"));

        // Assert
        Assert.Equal(2, memorySink.Entries.Count);
        Assert.Equal(LogLevel.Info, memorySink.Entries[0].Level);
        Assert.Equal("Major lifecycle event", memorySink.Entries[0].Message);
        Assert.Equal(LogLevel.Error, memorySink.Entries[1].Level);
        Assert.Equal("Operation failed", memorySink.Entries[1].Message);
        Assert.Contains("Boom", memorySink.FormattedLines[1]);
    }

    [Fact]
    public void AppLogger_DebugLevelEnabled_WritesDebugInfoAndError()
    {
        // Arrange (REQ-021)
        var memorySink = new InMemoryLogSink();
        using var logger = new AppLogger(LogLevel.Debug, [memorySink]);

        // Act
        logger.LogDebug("TestCategory", "Detailed debug trace");
        logger.LogInfo("TestCategory", "Major lifecycle event");
        logger.LogError("TestCategory", "Operation failed");

        // Assert
        Assert.Equal(3, memorySink.Entries.Count);
        Assert.Equal(LogLevel.Debug, memorySink.Entries[0].Level);
        Assert.Equal(LogLevel.Info, memorySink.Entries[1].Level);
        Assert.Equal(LogLevel.Error, memorySink.Entries[2].Level);
    }

    [Fact]
    public void AppLogger_ConsoleAndFileSinks_WritesFormattedEntriesToBothOutputs()
    {
        // Arrange (REQ-022)
        var tempDir = Path.Combine(Path.GetTempPath(), $"hm-log-test-{Guid.NewGuid():N}");
        var logFilePath = Path.Combine(tempDir, "logs", "housemonitor.log");
        using var consoleWriter = new StringWriter();

        var settings = new LoggingSettings
        {
            Level = "DEBUG",
            EnableConsole = true,
            EnableFile = true,
            FilePath = logFilePath
        };

        try
        {
            // Act
            using (var logger = AppLogger.Create(settings, consoleWriter))
            {
                logger.LogDebug("Startup", "Debug initialization details");
                logger.LogInfo("Startup", "Application started");
                logger.LogError("Network", "Connection lost", new HttpRequestException("Timeout"));
            }

            // Assert - Console output
            var consoleOutput = consoleWriter.ToString();
            Assert.Contains("[DEBUG] [Startup] Debug initialization details", consoleOutput);
            Assert.Contains("[INFO] [Startup] Application started", consoleOutput);
            Assert.Contains("[ERROR] [Network] Connection lost", consoleOutput);
            Assert.Contains("Timeout", consoleOutput);

            // Assert - File output
            Assert.True(File.Exists(logFilePath));
            var fileContent = File.ReadAllText(logFilePath);
            Assert.Contains("[DEBUG] [Startup] Debug initialization details", fileContent);
            Assert.Contains("[INFO] [Startup] Application started", fileContent);
            Assert.Contains("[ERROR] [Network] Connection lost", fileContent);
            Assert.Contains("Timeout", fileContent);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public void SecretConfigurationLoader_LoadsLoggingSettingsAndAppliesDefaultsWhenOmitted()
    {
        // Arrange (REQ-022)
        const string jsonWithLogging = """
            {
              "ApiSettings": {
                "EndpointUrl": "https://api.example.test/power/battery",
                "AuthHeaderName": "X-Auth-Key",
                "AuthKey": "test-secret-api-key"
              },
              "LoggingSettings": {
                "Level": "DEBUG",
                "EnableConsole": false,
                "EnableFile": true,
                "FilePath": "custom/debug.log"
              }
            }
            """;

        // Act
        var config = SecretConfigurationLoader.LoadConfigurationFromJson(jsonWithLogging);

        // Assert
        Assert.NotNull(config.LoggingSettings);
        Assert.Equal(LogLevel.Debug, config.LoggingSettings.GetParsedLogLevel());
        Assert.False(config.LoggingSettings.EnableConsole);
        Assert.True(config.LoggingSettings.EnableFile);
        Assert.Equal("custom/debug.log", config.LoggingSettings.FilePath);
    }

    [Fact]
    public void CommandLineOptions_ParsesLogSwitchesAndOverridesConfiguration()
    {
        // Arrange (REQ-022, REQ-023)
        var baseSettings = new LoggingSettings
        {
            Level = "INFO",
            EnableConsole = true,
            EnableFile = false,
            FilePath = "logs/default.log"
        };

        var args = new[] { "--console", "--debug", "--log-file", "logs/troubleshooting.log", "--no-console-log" };

        // Act
        var cliOptions = CommandLineOptions.Parse(args);
        var effectiveSettings = cliOptions.ApplyOverrides(baseSettings);

        // Assert
        Assert.True(cliOptions.ConsoleMode);
        Assert.Equal(LogLevel.Debug, effectiveSettings.GetParsedLogLevel());
        Assert.False(effectiveSettings.EnableConsole);
        Assert.True(effectiveSettings.EnableFile);
        Assert.Equal("logs/troubleshooting.log", effectiveSettings.FilePath);
    }

    [Fact]
    public async Task InternalProcesses_LogInfoDebugAndErrorAcrossClientViewModelAndPollingService()
    {
        // Arrange (REQ-021)
        var memorySink = new InMemoryLogSink();
        using var logger = new AppLogger(LogLevel.Debug, [memorySink]);

        var attempt = 0;
        var handler = new FakeHttpMessageHandler(_ =>
        {
            attempt++;
            if (attempt == 1)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(ValidSampleResponseJson, Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        });

        using var httpClient = new HttpClient(handler);
        var settings = new ApiSettings
        {
            EndpointUrl = "https://api.example.test/power/battery",
            AuthHeaderName = "X-Auth-Key",
            AuthKey = "super-secret-value"
        };

        var client = new PowerStatusClient(httpClient, settings, logger);
        var viewModel = new OverlayViewModel(client, logger);
        using var cts = new CancellationTokenSource();

        var pollingService = new PowerStatusPollingService(
            viewModel,
            TimeSpan.FromSeconds(5),
            delayAsync: (_, _) =>
            {
                if (attempt >= 2)
                {
                    cts.Cancel();
                }

                return Task.CompletedTask;
            },
            logger: logger);

        // Act
        await pollingService.RunAsync(cts.Token);

        // Assert - Verify DEBUG, INFO, and ERROR entries were recorded
        Assert.Contains(memorySink.Entries, e => e.Level == LogLevel.Debug && e.Category == nameof(PowerStatusClient));
        Assert.Contains(memorySink.Entries, e => e.Level == LogLevel.Info && e.Category == nameof(PowerStatusClient));
        Assert.Contains(memorySink.Entries, e => e.Level == LogLevel.Error && e.Category == nameof(PowerStatusClient));
        Assert.Contains(memorySink.Entries, e => e.Level == LogLevel.Info && e.Category == nameof(OverlayViewModel));
        Assert.Contains(memorySink.Entries, e => e.Level == LogLevel.Error && e.Category == nameof(OverlayViewModel));
        Assert.Contains(memorySink.Entries, e => e.Level == LogLevel.Info && e.Category == nameof(PowerStatusPollingService));

        // Ensure secret AuthKey is never leaked in any log message (REQ-009)
        Assert.DoesNotContain(memorySink.FormattedLines, line => line.Contains("super-secret-value", StringComparison.Ordinal));
    }

    [Fact]
    public void LoggingSettings_InvalidLevel_ThrowsInvalidOperationException()
    {
        // Arrange (REQ-021, REQ-022)
        var settings = new LoggingSettings { Level = "VERBOSE_INVALID" };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => settings.GetParsedLogLevel());
    }

    [Fact]
    public void ReadmeFile_DocumentsExecutionModesSwitchesAndLogLevelsInEnglish()
    {
        // Arrange (REQ-023)
        var repoRoot = SecretConfigurationLoader.ResolveSecretFilePath(
            AppContext.BaseDirectory,
            "HouseMonitor.sln");
        var rootDir = Path.GetDirectoryName(repoRoot)!;
        var readmePath = Path.Combine(rootDir, "README.md");

        // Act
        Assert.True(File.Exists(readmePath), $"Expected README.md at '{readmePath}'.");
        var content = File.ReadAllText(readmePath);

        // Assert
        Assert.Contains("--console", content);
        Assert.Contains("--debug", content);
        Assert.Contains("--log-level", content);
        Assert.Contains("--log-file", content);
        Assert.Contains("--no-console-log", content);
        Assert.Contains("ERROR", content);
        Assert.Contains("INFO", content);
        Assert.Contains("DEBUG", content);
        Assert.Contains("LoggingSettings", content);
    }

    [Fact]
    public void DevContainerConfiguration_DefinesDotNet8ImageGccAndAvaloniaX11Dependencies()
    {
        // Arrange (REQ-036)
        var repoRoot = SecretConfigurationLoader.ResolveSecretFilePath(
            AppContext.BaseDirectory,
            "HouseMonitor.sln");
        var rootDir = Path.GetDirectoryName(repoRoot)!;
        var devContainerJsonPath = Path.Combine(rootDir, ".devcontainer", "devcontainer.json");
        var dockerfilePath = Path.Combine(rootDir, ".devcontainer", "Dockerfile");

        // Act & Assert
        Assert.True(File.Exists(devContainerJsonPath), $"Expected devcontainer.json at '{devContainerJsonPath}'.");
        Assert.True(File.Exists(dockerfilePath), $"Expected Dockerfile at '{dockerfilePath}'.");

        var devContainerJson = File.ReadAllText(devContainerJsonPath);
        var dockerfile = File.ReadAllText(dockerfilePath);

        Assert.Contains("Dockerfile", devContainerJson);
        Assert.Contains("dotnet", devContainerJson);
        Assert.Contains("mcr.microsoft.com/devcontainers/dotnet", dockerfile);
        Assert.Contains("8.0", dockerfile);
        Assert.Contains("gcc", dockerfile);
        Assert.Contains("libx11-6", dockerfile);
        Assert.Contains("libice6", dockerfile);
        Assert.Contains("libsm6", dockerfile);
    }

    private sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(responder(request));
        }
    }
}
