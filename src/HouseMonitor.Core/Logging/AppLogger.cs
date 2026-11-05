using System.Globalization;
using System.Text;
using HouseMonitor.Core.Configuration;

namespace HouseMonitor.Core.Logging;

/// <summary>
/// Severity levels for internal process logging (REQ-021).
/// </summary>
public enum LogLevel
{
    Debug = 0,
    Info = 1,
    Error = 2
}

/// <summary>
/// Immutable representation of a structured log event (REQ-021).
/// </summary>
public sealed record LogEntry(
    DateTimeOffset Timestamp,
    LogLevel Level,
    string Category,
    string Message,
    Exception? Exception = null);

/// <summary>
/// Application logging contract supporting DEBUG, INFO, and ERROR severity levels (REQ-021).
/// </summary>
public interface IAppLogger
{
    LogLevel MinimumLevel { get; }

    bool IsEnabled(LogLevel level);

    void Log(LogLevel level, string category, string message, Exception? exception = null);

    void LogDebug(string category, string message)
        => Log(LogLevel.Debug, category, message);

    void LogInfo(string category, string message)
        => Log(LogLevel.Info, category, message);

    void LogError(string category, string message, Exception? exception = null)
        => Log(LogLevel.Error, category, message, exception);
}

/// <summary>
/// No-op logger implementation used when logging is not explicitly provided.
/// </summary>
public sealed class NullAppLogger : IAppLogger
{
    public static readonly NullAppLogger Instance = new();

    private NullAppLogger()
    {
    }

    public LogLevel MinimumLevel => LogLevel.Error;

    public bool IsEnabled(LogLevel level) => false;

    public void Log(LogLevel level, string category, string message, Exception? exception = null)
    {
    }
}

/// <summary>
/// Output destination for formatted log entries (REQ-022).
/// </summary>
public interface ILogSink : IDisposable
{
    void Write(LogEntry entry, string formattedLine);
}

/// <summary>
/// Writes formatted log entries to the console or a provided <see cref="TextWriter"/> (REQ-022).
/// </summary>
public sealed class ConsoleLogSink : ILogSink
{
    private readonly TextWriter _writer;
    private readonly object _syncRoot = new();

    public ConsoleLogSink(TextWriter? writer = null)
    {
        _writer = writer ?? Console.Out;
    }

    public void Write(LogEntry entry, string formattedLine)
    {
        lock (_syncRoot)
        {
            _writer.WriteLine(formattedLine);
            _writer.Flush();
        }
    }

    public void Dispose()
    {
    }
}

/// <summary>
/// Appends formatted log entries to a UTF-8 file on disk, creating parent directories if necessary (REQ-022).
/// </summary>
public sealed class FileLogSink : ILogSink
{
    private readonly string _filePath;
    private readonly object _syncRoot = new();

    public FileLogSink(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _filePath = Path.GetFullPath(filePath);

        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    public string FilePath => _filePath;

    public void Write(LogEntry entry, string formattedLine)
    {
        lock (_syncRoot)
        {
            File.AppendAllText(_filePath, formattedLine + Environment.NewLine, Encoding.UTF8);
        }
    }

    public void Dispose()
    {
    }
}

/// <summary>
/// Captures log entries and formatted lines in memory for diagnostics and testing (REQ-021, REQ-022).
/// </summary>
public sealed class InMemoryLogSink : ILogSink
{
    private readonly List<LogEntry> _entries = [];
    private readonly List<string> _formattedLines = [];
    private readonly object _syncRoot = new();

    public IReadOnlyList<LogEntry> Entries
    {
        get
        {
            lock (_syncRoot)
            {
                return _entries.ToList();
            }
        }
    }

    public IReadOnlyList<string> FormattedLines
    {
        get
        {
            lock (_syncRoot)
            {
                return _formattedLines.ToList();
            }
        }
    }

    public void Write(LogEntry entry, string formattedLine)
    {
        lock (_syncRoot)
        {
            _entries.Add(entry);
            _formattedLines.Add(formattedLine);
        }
    }

    public void Dispose()
    {
    }
}

/// <summary>
/// Concrete logger that filters by minimum <see cref="LogLevel"/> and dispatches formatted entries to configured sinks (REQ-021, REQ-022).
/// </summary>
public sealed class AppLogger : IAppLogger, IDisposable
{
    private readonly IReadOnlyList<ILogSink> _sinks;
    private readonly Func<DateTimeOffset> _clock;

    public AppLogger(
        LogLevel minimumLevel,
        IEnumerable<ILogSink> sinks,
        Func<DateTimeOffset>? clock = null)
    {
        ArgumentNullException.ThrowIfNull(sinks);

        MinimumLevel = minimumLevel;
        _sinks = sinks.ToList();
        _clock = clock ?? (() => DateTimeOffset.Now);
    }

    public LogLevel MinimumLevel { get; }

    public static AppLogger Create(
        LoggingSettings settings,
        TextWriter? consoleWriter = null,
        Func<DateTimeOffset>? clock = null)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var level = settings.GetParsedLogLevel();
        var sinks = new List<ILogSink>();

        if (settings.EnableConsole)
        {
            sinks.Add(new ConsoleLogSink(consoleWriter));
        }

        if (settings.EnableFile)
        {
            if (string.IsNullOrWhiteSpace(settings.FilePath))
            {
                throw new InvalidOperationException("A valid FilePath must be configured when file logging is enabled.");
            }

            sinks.Add(new FileLogSink(settings.FilePath));
        }

        return new AppLogger(level, sinks, clock);
    }

    public bool IsEnabled(LogLevel level) => level >= MinimumLevel;

    public void Log(LogLevel level, string category, string message, Exception? exception = null)
    {
        if (!IsEnabled(level))
        {
            return;
        }

        var entry = new LogEntry(
            _clock(),
            level,
            string.IsNullOrWhiteSpace(category) ? "General" : category,
            message ?? string.Empty,
            exception);

        var formatted = FormatEntry(entry);

        foreach (var sink in _sinks)
        {
            sink.Write(entry, formatted);
        }
    }

    public void LogDebug(string category, string message)
        => Log(LogLevel.Debug, category, message);

    public void LogInfo(string category, string message)
        => Log(LogLevel.Info, category, message);

    public void LogError(string category, string message, Exception? exception = null)
        => Log(LogLevel.Error, category, message, exception);

    public static string FormatEntry(LogEntry entry)
    {
        var levelTag = entry.Level switch
        {
            LogLevel.Debug => "DEBUG",
            LogLevel.Info => "INFO",
            LogLevel.Error => "ERROR",
            _ => entry.Level.ToString().ToUpperInvariant()
        };

        var timestampText = entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture);
        if (entry.Exception is null)
        {
            return $"{timestampText} [{levelTag}] [{entry.Category}] {entry.Message}";
        }

        return $"{timestampText} [{levelTag}] [{entry.Category}] {entry.Message} | Exception: {entry.Exception.GetType().Name}: {entry.Exception.Message}";
    }

    public void Dispose()
    {
        foreach (var sink in _sinks)
        {
            sink.Dispose();
        }
    }
}
