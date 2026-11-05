using System.Globalization;
using HouseMonitor.Core.Models;
using HouseMonitor.Core.Presentation;

namespace HouseMonitor.Tests.Presentation;

public class OverlayLayoutAndDisplayTests
{
    [Fact]
    public void CalculateBottomRightPosition_ReturnsCorrectCoordinatesWithMargin()
    {
        // Arrange (REQ-004)
        var screenWidth = 1920;
        var screenHeight = 1080;
        var windowWidth = 300;
        var windowHeight = 180;
        var margin = 16;

        // Act
        var position = OverlayPositionCalculator.CalculateBottomRight(
            screenWidth,
            screenHeight,
            windowWidth,
            windowHeight,
            margin);

        // Assert
        Assert.Equal(1604, position.X); // 1920 - 300 - 16
        Assert.Equal(884, position.Y);  // 1080 - 180 - 16
    }

    [Fact]
    public void FormatSummaryLines_ValidPowerStatus_RendersCompactReadableMetrics()
    {
        // Arrange (REQ-005)
        var timestamp = DateTimeOffset.Parse(
            "2026-11-04T22:20:35.014353+01:00",
            CultureInfo.InvariantCulture);
        var status = new PowerStatus(
            new BatteryStatus(65.0, "discharging"),
            new GeneratorStatus("stopped", 0.0),
            new SolarStatus(0.0, 17.98),
            new HouseStatus(415.0, 13.38),
            timestamp);

        // Act
        var lines = StatusDisplayFormatter.FormatSummaryLines(status);

        // Assert
        Assert.NotEmpty(lines);
        Assert.Contains(lines, l => l.Contains("65") && l.Contains("discharging", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(lines, l => l.Contains("17.98"));
        Assert.Contains(lines, l => l.Contains("415") && l.Contains("13.38"));
        Assert.Contains(lines, l => l.Contains("stopped", StringComparison.OrdinalIgnoreCase));
    }
}
