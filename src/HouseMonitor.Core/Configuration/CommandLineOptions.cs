using HouseMonitor.Core.Logging;

namespace HouseMonitor.Core.Configuration;

/// <summary>
/// Parses command-line switches for execution mode and logging overrides (REQ-022, REQ-023).
/// </summary>
public sealed class CommandLineOptions
{
    public bool ConsoleMode { get; init; }
    public bool ShowHelp { get; init; }
    public LogLevel? LogLevelOverride { get; init; }
    public bool? EnableConsoleLogOverride { get; init; }
    public string? LogFilePathOverride { get; init; }
    public string? SecretFilePathOverride { get; init; }

    public static CommandLineOptions Parse(string[]? args)
    {
        if (args is null || args.Length == 0)
        {
            return new CommandLineOptions();
        }

        var consoleMode = false;
        var showHelp = false;
        LogLevel? logLevelOverride = null;
        bool? enableConsoleLogOverride = null;
        string? logFilePathOverride = null;
        string? secretFilePathOverride = null;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (string.IsNullOrWhiteSpace(arg))
            {
                continue;
            }

            if (string.Equals(arg, "--console", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(arg, "-c", StringComparison.OrdinalIgnoreCase))
            {
                consoleMode = true;
            }
            else if (string.Equals(arg, "--help", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(arg, "-h", StringComparison.OrdinalIgnoreCase))
            {
                showHelp = true;
            }
            else if (string.Equals(arg, "--debug", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(arg, "-d", StringComparison.OrdinalIgnoreCase))
            {
                logLevelOverride = LogLevel.Debug;
            }
            else if (TryReadOptionValue(args, ref i, "--log-level", out var levelValue))
            {
                logLevelOverride = LoggingSettings.ParseLogLevel(levelValue);
            }
            else if (TryReadOptionValue(args, ref i, "--log-file", out var fileValue))
            {
                if (string.IsNullOrWhiteSpace(fileValue))
                {
                    throw new InvalidOperationException("Option '--log-file' requires a non-empty file path.");
                }

                logFilePathOverride = fileValue;
            }
            else if (string.Equals(arg, "--no-console-log", StringComparison.OrdinalIgnoreCase))
            {
                enableConsoleLogOverride = false;
            }
            else if (string.Equals(arg, "--console-log", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 < args.Length && bool.TryParse(args[i + 1], out var parsedBool))
                {
                    enableConsoleLogOverride = parsedBool;
                    i++;
                }
                else
                {
                    enableConsoleLogOverride = true;
                }
            }
            else if (arg.StartsWith("--console-log=", StringComparison.OrdinalIgnoreCase))
            {
                var valuePart = arg["--console-log=".Length..];
                if (!bool.TryParse(valuePart, out var parsedBool))
                {
                    throw new InvalidOperationException($"Invalid boolean value '{valuePart}' for '--console-log'.");
                }

                enableConsoleLogOverride = parsedBool;
            }
            else if (TryReadOptionValue(args, ref i, "--config", out var configPath) ||
                     TryReadOptionValue(args, ref i, "--secret-file", out configPath))
            {
                if (string.IsNullOrWhiteSpace(configPath))
                {
                    throw new InvalidOperationException("Option '--config' requires a non-empty file path.");
                }

                secretFilePathOverride = configPath;
            }
        }

        return new CommandLineOptions
        {
            ConsoleMode = consoleMode,
            ShowHelp = showHelp,
            LogLevelOverride = logLevelOverride,
            EnableConsoleLogOverride = enableConsoleLogOverride,
            LogFilePathOverride = logFilePathOverride,
            SecretFilePathOverride = secretFilePathOverride
        };
    }

    public LoggingSettings ApplyOverrides(LoggingSettings? baseSettings = null)
    {
        var current = baseSettings ?? new LoggingSettings();

        var effectiveLevel = LogLevelOverride switch
        {
            LogLevel.Debug => "DEBUG",
            LogLevel.Info => "INFO",
            LogLevel.Error => "ERROR",
            _ => current.Level
        };

        var effectiveEnableConsole = EnableConsoleLogOverride ?? current.EnableConsole;
        var effectiveEnableFile = !string.IsNullOrWhiteSpace(LogFilePathOverride) || current.EnableFile;
        var effectiveFilePath = !string.IsNullOrWhiteSpace(LogFilePathOverride)
            ? LogFilePathOverride!
            : current.FilePath;

        return new LoggingSettings
        {
            Level = effectiveLevel,
            EnableConsole = effectiveEnableConsole,
            EnableFile = effectiveEnableFile,
            FilePath = effectiveFilePath
        };
    }

    public static string GetHelpText() =>
        """
        HouseMonitor - Desktop Power Telemetry Overlay & CLI

        Usage:
          dotnet run --project src/HouseMonitor.App -- [options]

        Execution Modes:
          (default)                 Launch the graphical desktop overlay window.
          --console, -c             Fetch telemetry once, print summary to stdout, and exit.
          --help, -h                Show this command-line help message and exit.

        Configuration & Logging Switches:
          --config <path>           Path to a custom secret.json configuration file.
          --log-level <LEVEL>       Set minimum log level: DEBUG, INFO, or ERROR (default: INFO).
          --debug, -d               Shortcut for --log-level DEBUG (detailed diagnostic output).
          --log-file <path>         Enable file logging and write log entries to <path>.
          --console-log [bool]      Enable (or set true/false) log output to the console.
          --no-console-log          Disable log output to the console.
        """;

    private static bool TryReadOptionValue(string[] args, ref int index, string optionName, out string value)
    {
        var current = args[index];
        if (string.Equals(current, optionName, StringComparison.OrdinalIgnoreCase))
        {
            if (index + 1 >= args.Length)
            {
                throw new InvalidOperationException($"Option '{optionName}' requires a value.");
            }

            index++;
            value = args[index];
            return true;
        }

        var prefix = optionName + "=";
        if (current.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            value = current[prefix.Length..];
            return true;
        }

        value = string.Empty;
        return false;
    }
}
