using System.Globalization;
using HouseMonitor.Core.Clients;
using HouseMonitor.Core.Models;
using HouseMonitor.Core.Presentation;

namespace HouseMonitor.Tests.Presentation;

public class OverlayViewModelTests
{
    private static readonly DateTimeOffset PayloadTimestamp = DateTimeOffset.Parse(
        "2026-11-04T22:20:35.014353+01:00",
        CultureInfo.InvariantCulture);

    private static readonly PowerStatus SampleStatus = new(
        new BatteryStatus(65.0, "discharging"),
        new GeneratorStatus("stopped", 0.0),
        new SolarStatus(0.0, 17.98),
        new HouseStatus(415.0, 13.38),
        PayloadTimestamp);

    [Fact]
    public async Task RefreshOnceAsync_SuccessfulFetch_UpdatesTelemetryPropertiesAndUsesPayloadTimestamp()
    {
        // Arrange (REQ-005, REQ-015, REQ-016, REQ-019)
        var stubClient = new StubPowerStatusClient(() => Task.FromResult(SampleStatus));
        var viewModel = new OverlayViewModel(stubClient);
        var changedProperties = new List<string?>();
        viewModel.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        // Act
        await viewModel.RefreshOnceAsync();

        // Assert
        Assert.False(viewModel.HasError);
        Assert.Same(SampleStatus, viewModel.CurrentStatus);
        Assert.Equal(PayloadTimestamp, viewModel.LastUpdated);
        Assert.Contains("65", viewModel.BatteryText);
        Assert.Contains("discharging", viewModel.BatteryText);
        Assert.Contains("17.98", viewModel.SolarText);
        Assert.Contains("415", viewModel.HouseText);
        Assert.Contains("13.38", viewModel.HouseText);
        Assert.Contains("stopped", viewModel.GeneratorText);
        Assert.Contains("22:20:35", viewModel.StatusMessage);
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
        Assert.Equal(PayloadTimestamp, viewModel.LastUpdated);
        Assert.Contains("65", viewModel.BatteryText);
        Assert.Contains("415", viewModel.HouseText);
    }

    private sealed class StubPowerStatusClient(Func<Task<PowerStatus>> handler) : IPowerStatusClient
    {
        public Task<PowerStatus> GetPowerStatusAsync(CancellationToken cancellationToken = default)
            => handler();
    }
}
