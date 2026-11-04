using HouseMonitor.Core.Presentation;

namespace HouseMonitor.Core.Services;

/// <summary>
/// Periodically refreshes <see cref="OverlayViewModel"/> at a scheduled interval (REQ-016, REQ-017).
/// </summary>
public sealed class PowerStatusPollingService
{
    private readonly OverlayViewModel _viewModel;
    private readonly TimeSpan _refreshInterval;
    private readonly Func<TimeSpan, CancellationToken, Task> _delayAsync;

    public PowerStatusPollingService(
        OverlayViewModel viewModel,
        TimeSpan refreshInterval,
        Func<TimeSpan, CancellationToken, Task>? delayAsync = null)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        if (refreshInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(refreshInterval), "Refresh interval must be positive.");
        }

        _viewModel = viewModel;
        _refreshInterval = refreshInterval;
        _delayAsync = delayAsync ?? Task.Delay;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _viewModel.RefreshOnceAsync(cancellationToken).ConfigureAwait(false);
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                await _delayAsync(_refreshInterval, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}

