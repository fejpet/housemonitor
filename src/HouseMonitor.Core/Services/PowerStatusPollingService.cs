using HouseMonitor.Core.Logging;
using HouseMonitor.Core.Presentation;

namespace HouseMonitor.Core.Services;

/// <summary>
/// Periodically refreshes <see cref="OverlayViewModel"/> at a scheduled interval (REQ-016, REQ-017, REQ-021).
/// </summary>
public sealed class PowerStatusPollingService
{
    private readonly OverlayViewModel _viewModel;
    private readonly TimeSpan _refreshInterval;
    private readonly Func<TimeSpan, CancellationToken, Task> _delayAsync;
    private readonly IAppLogger _logger;

    public PowerStatusPollingService(
        OverlayViewModel viewModel,
        TimeSpan refreshInterval,
        Func<TimeSpan, CancellationToken, Task>? delayAsync = null,
        IAppLogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        if (refreshInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(refreshInterval), "Refresh interval must be positive.");
        }

        _viewModel = viewModel;
        _refreshInterval = refreshInterval;
        _delayAsync = delayAsync ?? Task.Delay;
        _logger = logger ?? NullAppLogger.Instance;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInfo(
            nameof(PowerStatusPollingService),
            $"Starting scheduled telemetry polling loop with interval {_refreshInterval.TotalSeconds:0.##}s.");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _viewModel.RefreshOnceAsync(cancellationToken).ConfigureAwait(false);
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                _logger.LogDebug(
                    nameof(PowerStatusPollingService),
                    $"Waiting {_refreshInterval.TotalSeconds:0.##}s before next telemetry poll.");

                await _delayAsync(_refreshInterval, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    nameof(PowerStatusPollingService),
                    "Unexpected error encountered in polling loop.",
                    ex);
            }
        }

        _logger.LogInfo(nameof(PowerStatusPollingService), "Scheduled telemetry polling loop stopped.");
    }
}
