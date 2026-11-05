using System.Globalization;
using HouseMonitor.Core.Models;

namespace HouseMonitor.Core.Presentation;

/// <summary>
/// Strongly-typed battery charging/discharging state (REQ-033).
/// </summary>
public enum BatteryState
{
    Idle,
    Charging,
    Discharging
}

/// <summary>
/// Strongly-typed backup generator operational state (REQ-033).
/// </summary>
public enum GeneratorState
{
    Stopped,
    Starting,
    Running,
    Fault
}

/// <summary>
/// Dark theme color palette and ergonomic HUD card dimensions (REQ-024, REQ-025, REQ-026).
/// </summary>
public static class DashboardThemePalette
{
    public const int CardWidthPx = 380;
    public const int CardMinWidthPx = 360;
    public const int CardMaxWidthPx = 400;
    public const int CardHeightPx = 292;
    public const double CardCornerRadius = 12.0;
    public const double InnerCardCornerRadius = 8.0;

    public const string MainBackgroundHex = "#0F172A";
    public const string CardBlockBackgroundHex = "#1E293B";
    public const string SubtleBorderHex = "#334155";
    public const string CyanAccentHex = "#38BDF8";
    public const string EmeraldGreenHex = "#10B981";
    public const string AmberWarningHex = "#F59E0B";
    public const string RedErrorHex = "#EF4444";
    public const string MutedSlateHex = "#64748B";
    public const string LabelSlateHex = "#94A3B8";
    public const string PrimaryValueHex = "#F8FAFC";
    public const string FooterTimestampHex = "#475569";
}

/// <summary>
/// Maps raw JSON state strings to strongly-typed <see cref="BatteryState"/> and <see cref="GeneratorState"/> enums (REQ-033).
/// </summary>
public static class TelemetryStateParser
{
    public static BatteryState ParseBatteryState(string? rawState)
    {
        if (string.IsNullOrWhiteSpace(rawState))
        {
            return BatteryState.Idle;
        }

        return rawState.Trim().ToLowerInvariant() switch
        {
            "charging" => BatteryState.Charging,
            "discharging" => BatteryState.Discharging,
            _ => BatteryState.Idle
        };
    }

    public static GeneratorState ParseGeneratorState(string? rawState)
    {
        if (string.IsNullOrWhiteSpace(rawState))
        {
            return GeneratorState.Stopped;
        }

        return rawState.Trim().ToLowerInvariant() switch
        {
            "starting" => GeneratorState.Starting,
            "running" => GeneratorState.Running,
            "fault" or "error" => GeneratorState.Fault,
            _ => GeneratorState.Stopped
        };
    }
}

/// <summary>
/// Determines the semantic color for the battery indicator and progress bar (REQ-025, REQ-028, REQ-035).
/// </summary>
public static class BatteryColorConverter
{
    public static string ConvertToHex(double batteryPercentage, BatteryState state = BatteryState.Idle)
    {
        if (batteryPercentage < 20.0)
        {
            return DashboardThemePalette.RedErrorHex;
        }

        if (batteryPercentage <= 50.0 || state == BatteryState.Discharging)
        {
            return DashboardThemePalette.AmberWarningHex;
        }

        return DashboardThemePalette.EmeraldGreenHex;
    }
}

/// <summary>
/// Formats <see cref="BatteryState"/> with a directional arrow indicator (REQ-028, REQ-035).
/// </summary>
public static class StateToIconOrArrowConverter
{
    public static string Convert(BatteryState state) => state switch
    {
        BatteryState.Discharging => "▼ discharging",
        BatteryState.Charging => "▲ charging",
        _ => "idle"
    };
}

/// <summary>
/// Selects active emerald green or muted slate gray for solar production (REQ-025, REQ-029, REQ-035).
/// </summary>
public static class ActiveSolarBrushConverter
{
    public static string ConvertToHex(bool isSolarProducing)
        => isSolarProducing ? DashboardThemePalette.EmeraldGreenHex : DashboardThemePalette.MutedSlateHex;
}

/// <summary>
/// Selects semantic color for the generator status badge (REQ-025, REQ-031).
/// </summary>
public static class GeneratorColorConverter
{
    public static string ConvertToHex(GeneratorState state) => state switch
    {
        GeneratorState.Running => DashboardThemePalette.EmeraldGreenHex,
        GeneratorState.Starting => DashboardThemePalette.AmberWarningHex,
        GeneratorState.Fault => DashboardThemePalette.RedErrorHex,
        _ => DashboardThemePalette.MutedSlateHex
    };
}

/// <summary>
/// Formats power in watts (or kilowatts when requested and &gt;= 1000 W) (REQ-029, REQ-030, REQ-035).
/// </summary>
public static class WattsToDisplayConverter
{
    public static string Convert(double watts, bool useKilowattsOver1000 = false)
    {
        if (useKilowattsOver1000 && Math.Abs(watts) >= 1000.0)
        {
            var kw = watts / 1000.0;
            return string.Create(CultureInfo.InvariantCulture, $"{kw:0.##} kW");
        }

        return string.Create(CultureInfo.InvariantCulture, $"{watts:0.##} W");
    }
}

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
/// Formats <see cref="PowerStatus"/> into compact, readable summary lines for the widget and CLI (REQ-005).
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
