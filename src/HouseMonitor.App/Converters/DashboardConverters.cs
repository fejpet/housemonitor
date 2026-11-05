using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using HouseMonitor.Core.Presentation;

namespace HouseMonitor.App.Converters;

/// <summary>
/// Avalonia value converter that converts a hex color string or battery SoC/state into a SolidColorBrush (REQ-025, REQ-028, REQ-035).
/// </summary>
public sealed class HexToBrushConverter : IValueConverter
{
    public static readonly HexToBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string hex && Color.TryParse(hex, out var color))
        {
            return new SolidColorBrush(color);
        }

        return new SolidColorBrush(Color.Parse(DashboardThemePalette.MutedSlateHex));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Avalonia value converter that maps <see cref="OverlayViewModel.IsSolarProducing"/> to active green or muted slate brush (REQ-029, REQ-035).
/// </summary>
public sealed class ActiveSolarBrushAvaloniaConverter : IValueConverter
{
    public static readonly ActiveSolarBrushAvaloniaConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isProducing = value is true;
        var hex = ActiveSolarBrushConverter.ConvertToHex(isProducing);
        return new SolidColorBrush(Color.Parse(hex));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Avalonia value converter that maps <see cref="BatteryState"/> to a directional arrow label (REQ-028, REQ-035).
/// </summary>
public sealed class StateToArrowTextConverter : IValueConverter
{
    public static readonly StateToArrowTextConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is BatteryState state)
        {
            return StateToIconOrArrowConverter.Convert(state);
        }

        return "idle";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Avalonia value converter that formats watts into a display string (REQ-029, REQ-030, REQ-035).
/// </summary>
public sealed class WattsToDisplayAvaloniaConverter : IValueConverter
{
    public static readonly WattsToDisplayAvaloniaConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double watts)
        {
            var useKw = string.Equals(parameter as string, "kW", StringComparison.OrdinalIgnoreCase);
            return WattsToDisplayConverter.Convert(watts, useKw);
        }

        return "0 W";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
