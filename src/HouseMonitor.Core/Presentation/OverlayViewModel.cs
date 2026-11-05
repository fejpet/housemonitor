using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using HouseMonitor.Core.Clients;
using HouseMonitor.Core.Logging;
using HouseMonitor.Core.Models;

namespace HouseMonitor.Core.Presentation;

/// <summary>
/// Observable ViewModel for the bottom-right HUD telemetry dashboard card (REQ-005, REQ-015, REQ-016, REQ-017, REQ-019, REQ-021, REQ-027..REQ-035).
/// </summary>
public class OverlayViewModel : INotifyPropertyChanged
{
    private readonly IPowerStatusClient _client;
    private readonly IAppLogger _logger;

    private PowerStatus? _currentStatus;
    private IReadOnlyList<string> _summaryLines = [];
    private string _batteryText = "Battery:   --";
    private string _solarText = "Solar:     --";
    private string _houseText = "House:     --";
    private string _generatorText = "Generator: --";
    private string _statusMessage = "Loading telemetry...";
    private bool _hasError;
    private bool _isOnline;
    private DateTimeOffset? _lastUpdated;

    private double _batteryPercentage;
    private BatteryState _batteryStatus = BatteryState.Idle;
    private double _solarPowerWatts;
    private double _solarTodayKWh;
    private double _housePowerWatts;
    private double _houseTodayKWh;
    private GeneratorState _generatorStatus = GeneratorState.Stopped;
    private double _generatorTodayKWh;

    public OverlayViewModel(IPowerStatusClient client, IAppLogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        _client = client;
        _logger = logger ?? NullAppLogger.Instance;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public PowerStatus? CurrentStatus
    {
        get => _currentStatus;
        private set => SetField(ref _currentStatus, value);
    }

    public IReadOnlyList<string> SummaryLines
    {
        get => _summaryLines;
        private set => SetField(ref _summaryLines, value);
    }

    public string BatteryText
    {
        get => _batteryText;
        private set => SetField(ref _batteryText, value);
    }

    public string SolarText
    {
        get => _solarText;
        private set => SetField(ref _solarText, value);
    }

    public string HouseText
    {
        get => _houseText;
        private set => SetField(ref _houseText, value);
    }

    public string GeneratorText
    {
        get => _generatorText;
        private set => SetField(ref _generatorText, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (SetField(ref _statusMessage, value))
            {
                OnPropertyChanged(nameof(FooterTimestampText));
            }
        }
    }

    public bool HasError
    {
        get => _hasError;
        private set => SetField(ref _hasError, value);
    }

    public bool IsOnline
    {
        get => _isOnline;
        set
        {
            if (SetField(ref _isOnline, value))
            {
                OnPropertyChanged(nameof(OnlineIndicatorHex));
            }
        }
    }

    public DateTimeOffset? LastUpdated
    {
        get => _lastUpdated;
        set
        {
            if (SetField(ref _lastUpdated, value))
            {
                OnPropertyChanged(nameof(HeaderTimeText));
                OnPropertyChanged(nameof(FooterTimestampText));
            }
        }
    }

    // --- HUD Dashboard Card Properties (REQ-028..REQ-035) ---

    public double BatteryPercentage
    {
        get => _batteryPercentage;
        set
        {
            if (SetField(ref _batteryPercentage, value))
            {
                OnPropertyChanged(nameof(BatteryPercentageDisplayText));
                OnPropertyChanged(nameof(BatteryColorHex));
            }
        }
    }

    public BatteryState BatteryStatus
    {
        get => _batteryStatus;
        set
        {
            if (SetField(ref _batteryStatus, value))
            {
                OnPropertyChanged(nameof(BatteryStatusDisplayText));
                OnPropertyChanged(nameof(BatteryColorHex));
            }
        }
    }

    public string BatteryPercentageDisplayText
        => string.Create(CultureInfo.InvariantCulture, $"{BatteryPercentage:0.#}%");

    public string BatteryStatusDisplayText
        => StateToIconOrArrowConverter.Convert(BatteryStatus);

    public string BatteryColorHex
        => BatteryColorConverter.ConvertToHex(BatteryPercentage, BatteryStatus);

    public double SolarPowerWatts
    {
        get => _solarPowerWatts;
        set
        {
            if (SetField(ref _solarPowerWatts, value))
            {
                OnPropertyChanged(nameof(IsSolarProducing));
                OnPropertyChanged(nameof(SolarPowerDisplayText));
                OnPropertyChanged(nameof(SolarForegroundHex));
            }
        }
    }

    public double SolarTodayKWh
    {
        get => _solarTodayKWh;
        set
        {
            if (SetField(ref _solarTodayKWh, value))
            {
                OnPropertyChanged(nameof(SolarTodayDisplayText));
            }
        }
    }

    public bool IsSolarProducing => SolarPowerWatts > 0;

    public string SolarPowerDisplayText
        => WattsToDisplayConverter.Convert(SolarPowerWatts);

    public string SolarTodayDisplayText
        => string.Create(CultureInfo.InvariantCulture, $"Today: {SolarTodayKWh:F2} kWh");

    public string SolarForegroundHex
        => ActiveSolarBrushConverter.ConvertToHex(IsSolarProducing);

    public double HousePowerWatts
    {
        get => _housePowerWatts;
        set
        {
            if (SetField(ref _housePowerWatts, value))
            {
                OnPropertyChanged(nameof(HousePowerDisplayText));
            }
        }
    }

    public double HouseTodayKWh
    {
        get => _houseTodayKWh;
        set
        {
            if (SetField(ref _houseTodayKWh, value))
            {
                OnPropertyChanged(nameof(HouseTodayDisplayText));
            }
        }
    }

    public string HousePowerDisplayText
        => WattsToDisplayConverter.Convert(HousePowerWatts);

    public string HouseTodayDisplayText
        => string.Create(CultureInfo.InvariantCulture, $"Today: {HouseTodayKWh:F2} kWh");

    public GeneratorState GeneratorStatus
    {
        get => _generatorStatus;
        set
        {
            if (SetField(ref _generatorStatus, value))
            {
                OnPropertyChanged(nameof(GeneratorStatusDisplayText));
                OnPropertyChanged(nameof(GeneratorForegroundHex));
            }
        }
    }

    public double GeneratorTodayKWh
    {
        get => _generatorTodayKWh;
        set
        {
            if (SetField(ref _generatorTodayKWh, value))
            {
                OnPropertyChanged(nameof(GeneratorTodayDisplayText));
            }
        }
    }

    public string GeneratorStatusDisplayText
        => GeneratorStatus.ToString().ToUpperInvariant();

    public string GeneratorForegroundHex
        => GeneratorColorConverter.ConvertToHex(GeneratorStatus);

    public string GeneratorTodayDisplayText
        => string.Create(CultureInfo.InvariantCulture, $"Today: {GeneratorTodayKWh:F1} kWh");

    public string OnlineIndicatorHex
        => IsOnline ? DashboardThemePalette.EmeraldGreenHex : DashboardThemePalette.RedErrorHex;

    public string HeaderTimeText
        => LastUpdated?.ToString("HH:mm:ss", CultureInfo.InvariantCulture) ?? "--:--:--";

    public string FooterTimestampText
        => HasError
            ? StatusMessage
            : LastUpdated is { } ts
                ? string.Create(CultureInfo.InvariantCulture, $"Timestamp: {ts:yyyy-MM-dd HH:mm:ss}")
                : StatusMessage;

    /// <summary>
    /// Fetches the latest telemetry from the REST endpoint and updates observable properties (REQ-016, REQ-017, REQ-019, REQ-021, REQ-034).
    /// </summary>
    public async Task RefreshOnceAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(nameof(OverlayViewModel), "Starting telemetry refresh.");

        try
        {
            var status = await _client.GetPowerStatusAsync(cancellationToken).ConfigureAwait(false);
            var lines = StatusDisplayFormatter.FormatSummaryLines(status);
            var timestamp = status.Timestamp;

            CurrentStatus = status;
            SummaryLines = lines;
            BatteryText = lines[0];
            SolarText = lines[1];
            HouseText = lines[2];
            GeneratorText = lines[3];

            BatteryPercentage = status.Battery.Soc;
            BatteryStatus = TelemetryStateParser.ParseBatteryState(status.Battery.State);
            SolarPowerWatts = status.Solar.Production;
            SolarTodayKWh = status.Solar.Today;
            HousePowerWatts = status.House.Consumption;
            HouseTodayKWh = status.House.TodayConsumption;
            GeneratorStatus = TelemetryStateParser.ParseGeneratorState(status.Generator.State);
            GeneratorTodayKWh = status.Generator.Today;

            HasError = false;
            IsOnline = true;
            LastUpdated = timestamp;
            StatusMessage = string.Create(CultureInfo.InvariantCulture, $"Timestamp: {timestamp:yyyy-MM-dd HH:mm:ss}");

            _logger.LogInfo(
                nameof(OverlayViewModel),
                string.Create(CultureInfo.InvariantCulture, $"Overlay ViewModel updated with telemetry timestamp {timestamp:yyyy-MM-dd HH:mm:ss}."));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug(nameof(OverlayViewModel), "Telemetry refresh cancelled.");
            throw;
        }
        catch (Exception ex)
        {
            HasError = true;
            IsOnline = false;
            StatusMessage = $"Update failed: {ex.Message}";
            _logger.LogError(
                nameof(OverlayViewModel),
                $"Telemetry refresh failed; preserving last known state. Reason: {ex.Message}",
                ex);
        }
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Named alias matching the specification's HouseMonitorViewModel (REQ-034).
/// </summary>
public sealed class HouseMonitorViewModel(IPowerStatusClient client, IAppLogger? logger = null)
    : OverlayViewModel(client, logger);
