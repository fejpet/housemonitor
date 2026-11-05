using System.Globalization;
using HouseMonitor.Core.Clients;
using HouseMonitor.Core.Models;
using HouseMonitor.Core.Presentation;

namespace HouseMonitor.Tests.Presentation;

public class TelemetryDashboardWidgetTests
{
    private static readonly DateTimeOffset SampleTimestamp = DateTimeOffset.Parse(
        "2026-11-04T22:20:35.014353+01:00",
        CultureInfo.InvariantCulture);

    [Theory]
    [InlineData("discharging", BatteryState.Discharging)]
    [InlineData("DISCHARGING", BatteryState.Discharging)]
    [InlineData("charging", BatteryState.Charging)]
    [InlineData("Charging", BatteryState.Charging)]
    [InlineData("idle", BatteryState.Idle)]
    [InlineData("", BatteryState.Idle)]
    public void ParseBatteryState_MapsJsonStringToStronglyTypedEnum(string rawState, BatteryState expected)
    {
        // Act (REQ-033)
        var result = TelemetryStateParser.ParseBatteryState(rawState);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("stopped", GeneratorState.Stopped)]
    [InlineData("STOPPED", GeneratorState.Stopped)]
    [InlineData("starting", GeneratorState.Starting)]
    [InlineData("running", GeneratorState.Running)]
    [InlineData("fault", GeneratorState.Fault)]
    [InlineData("error", GeneratorState.Fault)]
    public void ParseGeneratorState_MapsJsonStringToStronglyTypedEnum(string rawState, GeneratorState expected)
    {
        // Act (REQ-033)
        var result = TelemetryStateParser.ParseGeneratorState(rawState);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(15.0, BatteryState.Idle, "#EF4444")]        // Critical (<20%) -> Red
    [InlineData(19.9, BatteryState.Charging, "#EF4444")]    // Critical (<20%) -> Red
    [InlineData(35.0, BatteryState.Idle, "#F59E0B")]        // Medium (20-50%) -> Amber
    [InlineData(83.0, BatteryState.Discharging, "#F59E0B")] // Discharging -> Amber
    [InlineData(83.0, BatteryState.Charging, "#10B981")]    // >50% Charging -> Emerald Green
    [InlineData(65.0, BatteryState.Idle, "#10B981")]        // >50% Idle -> Emerald Green
    public void BatteryColorConverter_ReturnsExpectedSemanticHexColor(
        double soc,
        BatteryState state,
        string expectedHex)
    {
        // Act (REQ-025, REQ-028, REQ-035)
        var colorHex = BatteryColorConverter.ConvertToHex(soc, state);

        // Assert
        Assert.Equal(expectedHex, colorHex);
    }

    [Theory]
    [InlineData(BatteryState.Discharging, "▼ discharging")]
    [InlineData(BatteryState.Charging, "▲ charging")]
    [InlineData(BatteryState.Idle, "idle")]
    public void StateToIconOrArrowConverter_ReturnsDirectionalArrowText(
        BatteryState state,
        string expectedText)
    {
        // Act (REQ-028, REQ-035)
        var formatted = StateToIconOrArrowConverter.Convert(state);

        // Assert
        Assert.Equal(expectedText, formatted);
    }

    [Theory]
    [InlineData(false, "#64748B")] // Inactive 0 W -> Muted Slate Gray
    [InlineData(true, "#10B981")]  // Active > 0 W -> Emerald Green
    public void ActiveSolarBrushConverter_ReturnsDynamicColorForSolarActivity(
        bool isProducing,
        string expectedHex)
    {
        // Act (REQ-025, REQ-029, REQ-035)
        var colorHex = ActiveSolarBrushConverter.ConvertToHex(isProducing);

        // Assert
        Assert.Equal(expectedHex, colorHex);
    }

    [Theory]
    [InlineData(0.0, false, "0 W")]
    [InlineData(454.0, false, "454 W")]
    [InlineData(2450.0, false, "2450 W")]
    [InlineData(2450.0, true, "2.45 kW")]
    public void WattsToDisplayConverter_FormatsWattsAndOptionalKilowatts(
        double watts,
        bool useKilowattsOver1000,
        string expected)
    {
        // Act (REQ-029, REQ-030, REQ-035)
        var formatted = WattsToDisplayConverter.Convert(watts, useKilowattsOver1000);

        // Assert
        Assert.Equal(expected, formatted);
    }

    [Fact]
    public async Task OverlayViewModel_ExposesReactiveHudPropertiesAndUpdatesOnlineAndSolarState()
    {
        // Arrange (REQ-027, REQ-028, REQ-029, REQ-030, REQ-031, REQ-032, REQ-034)
        var status = new PowerStatus(
            new BatteryStatus(83.0, "discharging"),
            new GeneratorStatus("stopped", 0.0),
            new SolarStatus(0.0, 11.63),
            new HouseStatus(454.0, 6.69),
            SampleTimestamp);

        var stubClient = new StubClient(() => Task.FromResult(status));
        var viewModel = new HouseMonitorViewModel(stubClient);
        var changedProps = new HashSet<string?>();
        viewModel.PropertyChanged += (_, e) => changedProps.Add(e.PropertyName);

        // Act
        await viewModel.RefreshOnceAsync();

        // Assert - Reactive HUD properties
        Assert.True(viewModel.IsOnline);
        Assert.Equal(83.0, viewModel.BatteryPercentage);
        Assert.Equal(BatteryState.Discharging, viewModel.BatteryStatus);
        Assert.Equal("▼ discharging", viewModel.BatteryStatusDisplayText);
        Assert.Equal("#F59E0B", viewModel.BatteryColorHex);

        Assert.Equal(0.0, viewModel.SolarPowerWatts);
        Assert.Equal(11.63, viewModel.SolarTodayKWh);
        Assert.False(viewModel.IsSolarProducing);
        Assert.Equal("#64748B", viewModel.SolarForegroundHex);
        Assert.Equal("Today: 11.63 kWh", viewModel.SolarTodayDisplayText);

        Assert.Equal(454.0, viewModel.HousePowerWatts);
        Assert.Equal(6.69, viewModel.HouseTodayKWh);
        Assert.Equal("Today: 6.69 kWh", viewModel.HouseTodayDisplayText);

        Assert.Equal(GeneratorState.Stopped, viewModel.GeneratorStatus);
        Assert.Equal(0.0, viewModel.GeneratorTodayKWh);
        Assert.Equal("STOPPED", viewModel.GeneratorStatusDisplayText);
        Assert.Equal("Today: 0.0 kWh", viewModel.GeneratorTodayDisplayText);

        Assert.Equal("22:20:35", viewModel.HeaderTimeText);
        Assert.Equal("Timestamp: 2026-11-04 22:20:35", viewModel.FooterTimestampText);

        Assert.Contains(nameof(OverlayViewModel.BatteryPercentage), changedProps);
        Assert.Contains(nameof(OverlayViewModel.BatteryStatus), changedProps);
        Assert.Contains(nameof(OverlayViewModel.SolarTodayKWh), changedProps);
        Assert.Contains(nameof(OverlayViewModel.HousePowerWatts), changedProps);
        Assert.Contains(nameof(OverlayViewModel.IsOnline), changedProps);
    }

    [Fact]
    public async Task OverlayViewModel_WhenSolarStartsProducing_SwitchesIsSolarProducingAndActiveColor()
    {
        // Arrange (REQ-029, REQ-034, REQ-035)
        var activeSolarStatus = new PowerStatus(
            new BatteryStatus(88.0, "charging"),
            new GeneratorStatus("running", 2.4),
            new SolarStatus(2450.0, 14.20),
            new HouseStatus(510.0, 7.10),
            SampleTimestamp);

        var viewModel = new OverlayViewModel(new StubClient(() => Task.FromResult(activeSolarStatus)));

        // Act
        await viewModel.RefreshOnceAsync();

        // Assert
        Assert.True(viewModel.IsSolarProducing);
        Assert.Equal("#10B981", viewModel.SolarForegroundHex);
        Assert.Equal(BatteryState.Charging, viewModel.BatteryStatus);
        Assert.Equal("▲ charging", viewModel.BatteryStatusDisplayText);
        Assert.Equal("#10B981", viewModel.BatteryColorHex);
        Assert.Equal(GeneratorState.Running, viewModel.GeneratorStatus);
        Assert.Equal("RUNNING", viewModel.GeneratorStatusDisplayText);
        Assert.Equal("Today: 2.4 kWh", viewModel.GeneratorTodayDisplayText);
    }

    [Fact]
    public void DashboardThemePalette_MatchesErgonomicDimensionsAndDarkThemeColors()
    {
        // Assert (REQ-024, REQ-025, REQ-026)
        Assert.InRange(DashboardThemePalette.CardWidthPx, 360, 400);
        Assert.InRange(DashboardThemePalette.CardHeightPx, 280, 310);
        Assert.Equal("#0F172A", DashboardThemePalette.MainBackgroundHex);
        Assert.Equal("#1E293B", DashboardThemePalette.CardBlockBackgroundHex);
        Assert.Equal("#334155", DashboardThemePalette.SubtleBorderHex);
        Assert.Equal("#38BDF8", DashboardThemePalette.CyanAccentHex);
        Assert.Equal("#10B981", DashboardThemePalette.EmeraldGreenHex);
        Assert.Equal("#F59E0B", DashboardThemePalette.AmberWarningHex);
        Assert.Equal("#EF4444", DashboardThemePalette.RedErrorHex);
        Assert.Equal("#64748B", DashboardThemePalette.MutedSlateHex);
    }

    [Fact]
    public void HouseMonitorCardXaml_ConstrainedBatteryProgressBarAndAutoSizedWindowPreventOverflowAndClipping()
    {
        // Arrange (REQ-024, REQ-028, REQ-032)
        var repoRoot = HouseMonitor.Core.Configuration.SecretConfigurationLoader.ResolveSecretFilePath(
            AppContext.BaseDirectory,
            "HouseMonitor.sln");
        var rootDir = Path.GetDirectoryName(repoRoot)!;
        var xamlPath = Path.Combine(rootDir, "src", "HouseMonitor.App", "Views", "HouseMonitorCard.axaml");
        var windowPath = Path.Combine(rootDir, "src", "HouseMonitor.App", "OverlayWindow.cs");

        // Act
        var xaml = File.ReadAllText(xamlPath);
        var windowCode = File.ReadAllText(windowPath);

        // Assert - ProgressBar overrides Avalonia FluentTheme's default MinWidth=200 so it fits inside the card
        Assert.Contains("<ProgressBar", xaml);
        Assert.Contains("MinWidth=\"0\"", xaml);
        Assert.Contains("HorizontalAlignment=\"Stretch\"", xaml);

        // Assert - OverlayWindow auto-sizes height to content so the footer timestamp is never clipped
        Assert.Contains("SizeToContent.Height", windowCode);
    }

    private sealed class StubClient(Func<Task<PowerStatus>> callback) : IPowerStatusClient
    {
        public Task<PowerStatus> GetPowerStatusAsync(CancellationToken cancellationToken = default)
            => callback();
    }
}
