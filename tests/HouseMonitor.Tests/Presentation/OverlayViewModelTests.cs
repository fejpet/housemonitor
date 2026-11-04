using HouseMonitor.Core.Clients;
using HouseMonitor.Core.Models;
using HouseMonitor.Core.Presentation;

namespace HouseMonitor.Tests.Presentation;

public class OverlayViewModelTests
{
    private static readonly PowerStatus SampleStatus = new(
        new BatteryStatus(69.0, "discharging"),
        new GeneratorStatus("stopped", 0.0),
        new SolarStatus(0.0, 17.98),
        new HouseStatus(414.0, 12.84));

    [Fact]
    public async Task RefreshOnceAsync_SuccessfulFetch_UpdatesTelemetryPropertiesAndNotifiesListeners()
    {
        // Arrange (REQ-005, REQ-015, REQ-016)
        var stubClient = new StubPowerStatusClient(() => Task.FromResult(SampleStatus));
        var fixedNow = new DateTimeOffset(2026, 11, 4, 21, 55, 12, TimeSpan.FromHours(1));
        var viewModel = new OverlayViewModel(stubClient, () => fixedNow);
        var changedProperties = new List<string?>();
        viewModel.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        // Act
        await viewModel.RefreshOnceAsync();

        // Assert
        Assert.False(viewModel.HasError);
        Assert.Same(SampleStatus, viewModel.CurrentStatus);
        Assert.Equal(fixedNow, viewModel.LastUpdated);
        Assert.Contains("69", viewModel.BatteryText);
        Assert.Contains("discharging", viewModel.BatteryText);
        Assert.Contains("17.98", viewModel.SolarText);
        Assert.Contains("414", viewModel.HouseText);
        Assert.Contains("12.84", viewModel.HouseText);
        Assert.Contains("stopped", viewModel.GeneratorText);
        Assert.Contains("21:55:12", viewModel.StatusMessage);
        Assert.Equal(4, viewModel.SummaryLines.Count);
        Assert.NotEmpty(changedProperties);
    }

    [Fact]
    public async Task RefreshOnceAsync_WhenClientFails_PreservesLastKnownStatusAndExposesError()
    {
        // Arrange (REQ-017)
        var shouldFail = false;
        var stubClient = new StubPowerStatusClient(() =>
        {
            if (shouldFail)
            {
                throw new HttpRequestException("Connection refused");
            }

            return Task.FromResult(SampleStatus);
        });

        var viewModel = new OverlayViewModel(stubClient);
        await viewModel.RefreshOnceAsync();

        // Act - simulate network failure on next poll
        shouldFail = true;
        await viewModel.RefreshOnceAsync();

        // Assert
        Assert.True(viewModel.HasError);
        Assert.Contains("Connection refused", viewModel.StatusMessage);
        Assert.Same(SampleStatus, viewModel.CurrentStatus);
        Assert.Contains("69", viewModel.BatteryText);
        Assert.Contains("414", viewModel.HouseText);
    }

    private sealed class StubPowerStatusClient(Func<Task<PowerStatus>> handler) : IPowerStatusClient
    {
        public Task<PowerStatus> GetPowerStatusAsync(CancellationToken cancellationToken = default)
            => handler();
    }
}

