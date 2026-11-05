using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using HouseMonitor.App.Views;
using HouseMonitor.Core.Presentation;

namespace HouseMonitor.App;

/// <summary>
/// Compact bottom-right desktop overlay window hosting the modern <see cref="HouseMonitorCard"/> HUD widget (REQ-004, REQ-015, REQ-018, REQ-024..REQ-035).
/// </summary>
public sealed class OverlayWindow : Window
{
    private const int ScreenMarginPx = 16;

    public OverlayWindow(OverlayViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        Title = "HouseMonitor";
        Width = DashboardThemePalette.CardWidthPx;
        MinHeight = DashboardThemePalette.CardHeightPx;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        Topmost = true;
        SystemDecorations = SystemDecorations.None;
        TransparencyLevelHint = [WindowTransparencyLevel.Transparent, WindowTransparencyLevel.None];
        Background = Brushes.Transparent;
        WindowStartupLocation = WindowStartupLocation.Manual;

        var card = new HouseMonitorCard(viewModel);
        card.CloseRequested += (_, _) => Close();
        Content = card;

        PointerPressed += OnWindowPointerPressed;
        Opened += OnWindowOpened;
        SizeChanged += OnWindowSizeChanged;
    }

    private void OnWindowOpened(object? sender, EventArgs e)
    {
        PositionAtBottomRight();
    }

    private void OnWindowSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        PositionAtBottomRight();
    }

    private void OnWindowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void PositionAtBottomRight()
    {
        var primaryScreen = Screens.Primary ?? Screens.All.FirstOrDefault();
        if (primaryScreen is null)
        {
            return;
        }

        var workArea = primaryScreen.WorkingArea;
        var scaling = primaryScreen.Scaling > 0 ? primaryScreen.Scaling : 1.0;
        var effectiveWidth = Bounds.Width > 0 ? Bounds.Width : Width;
        var effectiveHeight = Bounds.Height > 0 ? Bounds.Height : MinHeight;

        var pixelWidth = (int)Math.Round(effectiveWidth * scaling);
        var pixelHeight = (int)Math.Round(effectiveHeight * scaling);
        var pixelMargin = (int)Math.Round(ScreenMarginPx * scaling);

        var offset = OverlayPositionCalculator.CalculateBottomRight(
            workArea.Width,
            workArea.Height,
            pixelWidth,
            pixelHeight,
            pixelMargin);

        Position = new PixelPoint(workArea.X + offset.X, workArea.Y + offset.Y);
    }
}
