using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using HouseMonitor.Core.Clients;
using HouseMonitor.Core.Models;

namespace HouseMonitor.Core.Presentation;

/// <summary>
/// Observable ViewModel for the bottom-right desktop overlay window (REQ-005, REQ-015, REQ-016, REQ-017, REQ-019).
/// </summary>
public sealed class OverlayViewModel : INotifyPropertyChanged
{
    private readonly IPowerStatusClient _client;

    private PowerStatus? _currentStatus;
    private IReadOnlyList<string> _summaryLines = [];
    private string _batteryText = "Battery:   --";
    private string _solarText = "Solar:     --";
    private string _houseText = "House:     --";
    private string _generatorText = "Generator: --";
    private string _statusMessage = "Loading telemetry...";
    private bool _hasError;
    private DateTimeOffset? _lastUpdated;

    public OverlayViewModel(IPowerStatusClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        _client = client;
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
        private set => SetField(ref _statusMessage, value);
    }

    public bool HasError
    {
        get => _hasError;
        private set => SetField(ref _hasError, value);
    }

    public DateTimeOffset? LastUpdated
    {
        get => _lastUpdated;
        private set => SetField(ref _lastUpdated, value);
    }

    /// <summary>
    /// Fetches the latest telemetry from the REST endpoint and updates observable properties (REQ-016, REQ-017, REQ-019).
    /// </summary>
    public async Task RefreshOnceAsync(CancellationToken cancellationToken = default)
    {
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
            LastUpdated = timestamp;
            HasError = false;
            StatusMessage = string.Create(CultureInfo.InvariantCulture, $"Timestamp: {timestamp:yyyy-MM-dd HH:mm:ss}");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            HasError = true;
            StatusMessage = $"Update failed: {ex.Message}";
        }
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
