using System.Globalization;
using HouseMonitor.Core.Models;

namespace HouseMonitor.Core.Presentation;

/// <summary>
/// Screen coordinates for positioning the monitor overlay window (REQ-004).
/// </summary>
public readonly record struct ScreenPosition(int X, int Y);

/// <summary>
/// Calculates the bottom-right monitor coordinates for the overlay window (REQ-004).
/// </summary>
public static class OverlayPositionCalculator
{
    public static ScreenPosition CalculateBottomRight(
        int screenWidth,
        int screenHeight,
        int windowWidth,
        int windowHeight,
        int margin = 16)
    {
        var x = Math.Max(0, screenWidth - windowWidth - margin);
        var y = Math.Max(0, screenHeight - windowHeight - margin);
        return new ScreenPosition(x, y);
    }
}

/// <summary>
/// Formats <see cref="PowerStatus"/> into compact, readable summary lines for the widget (REQ-005).
/// </summary>
public static class StatusDisplayFormatter
{
    public static IReadOnlyList<string> FormatSummaryLines(PowerStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);

        return
        [
            string.Create(CultureInfo.InvariantCulture,
                $"Battery:   {status.Battery.Soc:0.#}% ({status.Battery.State})"),
            string.Create(CultureInfo.InvariantCulture,
                $"Solar:     {status.Solar.Production:0.##} W (Today: {status.Solar.Today:0.##} kWh)"),
            string.Create(CultureInfo.InvariantCulture,
                $"House:     {status.House.Consumption:0.#} W (Today: {status.House.TodayConsumption:0.##} kWh)"),
            string.Create(CultureInfo.InvariantCulture,
                $"Generator: {status.Generator.State} (Today: {status.Generator.Today:0.##} kWh)")
        ];
    }
}

