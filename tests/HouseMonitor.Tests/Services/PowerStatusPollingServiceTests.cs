using System.Globalization;
using HouseMonitor.Core.Clients;
using HouseMonitor.Core.Models;
using HouseMonitor.Core.Presentation;
using HouseMonitor.Core.Services;

namespace HouseMonitor.Tests.Services;

public class PowerStatusPollingServiceTests
{
    private static readonly PowerStatus InitialStatus = new(
        new BatteryStatus(65.0, "discharging"),
        new GeneratorStatus("stopped", 0.0),
        new SolarStatus(0.0, 17.98),
        new HouseStatus(415.0, 13.38),
        DateTimeOffset.Parse("2026-11-04T22:20:35.014353+01:00", CultureInfo.InvariantCulture));

    private static readonly PowerStatus UpdatedStatus = new(
        new BatteryStatus(72.5, "charging"),
        new GeneratorStatus("stopped", 0.0),
        new SolarStatus(850.0, 18.1),
        new HouseStatus(310.0, 13.5),
        DateTimeOffset.Parse("2026-11-04T22:20:45.014353+01:00", CultureInfo.InvariantCulture));

    [Fact]
    public async Task RunAsync_PollsImmediatelyAndOnEachScheduledIntervalUntilCancelled()
    {
        // Arrange (REQ-016)
        var callCount = 0;
        using var cts = new CancellationTokenSource();

        var stubClient = new CallbackPowerStatusClient(() =>
        {
            callCount++;
            return Task.FromResult(callCount == 1 ? InitialStatus : UpdatedStatus);
        });

        var viewModel = new OverlayViewModel(stubClient);
        var delayIntervals = new List<TimeSpan>();

        var service = new PowerStatusPollingService(
            viewModel,
            TimeSpan.FromSeconds(10),
            delayAsync: (interval, token) =>
            {
                delayIntervals.Add(interval);
                if (delayIntervals.Count >= 2)
                {
                    cts.Cancel();
                    token.ThrowIfCancellationRequested();
                }

                return Task.CompletedTask;
            });

        // Act
        await service.RunAsync(cts.Token);

        // Assert
        Assert.Equal(2, callCount);
        Assert.All(delayIntervals, i => Assert.Equal(TimeSpan.FromSeconds(10), i));
        Assert.Same(UpdatedStatus, viewModel.CurrentStatus);
        Assert.Contains("72.5", viewModel.BatteryText);
    }

    [Fact]
    public async Task RunAsync_WhenFetchThrows_ContinuesPollingNextIntervals()
    {
        // Arrange (REQ-017)
        var callCount = 0;
        using var cts = new CancellationTokenSource();

        var stubClient = new CallbackPowerStatusClient(() =>
        {
            callCount++;
            if (callCount == 1)
            {
                throw new HttpRequestException("Temporary network failure");
            }

            cts.Cancel();
            return Task.FromResult(UpdatedStatus);
        });

        var viewModel = new OverlayViewModel(stubClient);
        var service = new PowerStatusPollingService(
            viewModel,
            TimeSpan.FromSeconds(5),
            delayAsync: (_, _) => Task.CompletedTask);

        // Act
        await service.RunAsync(cts.Token);

        // Assert
        Assert.Equal(2, callCount);
        Assert.False(viewModel.HasError);
        Assert.Same(UpdatedStatus, viewModel.CurrentStatus);
    }

    private sealed class CallbackPowerStatusClient(Func<Task<PowerStatus>> callback) : IPowerStatusClient
    {
        public Task<PowerStatus> GetPowerStatusAsync(CancellationToken cancellationToken = default)
            => callback();
    }
}
