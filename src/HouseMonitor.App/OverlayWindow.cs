using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using HouseMonitor.Core.Presentation;

namespace HouseMonitor.App;

/// <summary>
/// Compact bottom-right desktop overlay window displaying live house power telemetry (REQ-004, REQ-005, REQ-015, REQ-018).
/// </summary>
public sealed class OverlayWindow : Window
{
    private const int WindowWidthPx = 310;
    private const int WindowHeightPx = 155;
    private const int ScreenMarginPx = 16;

    private readonly OverlayViewModel _viewModel;
    private readonly TextBlock _batteryTextBlock;
    private readonly TextBlock _solarTextBlock;
    private readonly TextBlock _houseTextBlock;
    private readonly TextBlock _generatorTextBlock;
    private readonly TextBlock _statusTextBlock;

    public OverlayWindow(OverlayViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        _viewModel = viewModel;

        Title = "HouseMonitor";
        Width = WindowWidthPx;
        Height = WindowHeightPx;
        CanResize = false;
        Topmost = true;
        SystemDecorations = SystemDecorations.None;
        TransparencyLevelHint = [WindowTransparencyLevel.Transparent, WindowTransparencyLevel.None];
        Background = Brushes.Transparent;
        WindowStartupLocation = WindowStartupLocation.Manual;

        _batteryTextBlock = CreateMetricTextBlock();
        _solarTextBlock = CreateMetricTextBlock();
        _houseTextBlock = CreateMetricTextBlock();
        _generatorTextBlock = CreateMetricTextBlock();
        _statusTextBlock = new TextBlock
        {
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.Parse("#94A3B8")),
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        var headerGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            Margin = new Thickness(0, 0, 0, 6)
        };

        var titleText = new TextBlock
        {
            Text = "HOUSE MONITOR",
            FontSize = 11,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(Color.Parse("#38BDF8")),
            VerticalAlignment = VerticalAlignment.Center
        };

        var closeButton = new Button
        {
            Content = "×",
            FontSize = 13,
            Padding = new Thickness(6, 0),
            Background = Brushes.Transparent,
            Foreground = new SolidColorBrush(Color.Parse("#94A3B8")),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        closeButton.Click += (_, _) => Close();

        Grid.SetColumn(titleText, 0);
        Grid.SetColumn(closeButton, 1);
        headerGrid.Children.Add(titleText);
        headerGrid.Children.Add(closeButton);

        var contentStack = new StackPanel
        {
            Spacing = 3,
            Children =
            {
                headerGrid,
                _batteryTextBlock,
                _solarTextBlock,
                _houseTextBlock,
                _generatorTextBlock,
                new Border
                {
                    Height = 1,
                    Background = new SolidColorBrush(Color.Parse("#334155")),
                    Margin = new Thickness(0, 4, 0, 2)
                },
                _statusTextBlock
            }
        };

        Content = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#E60F172A")),
            BorderBrush = new SolidColorBrush(Color.Parse("#334155")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(12, 8),
            Child = contentStack
        };

        PointerPressed += OnWindowPointerPressed;
        Opened += OnWindowOpened;
        Closed += OnWindowClosed;

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        ApplyViewModelState();
    }

    private static TextBlock CreateMetricTextBlock() => new()
    {
        FontFamily = new FontFamily("monospace"),
        FontSize = 12,
        Foreground = new SolidColorBrush(Color.Parse("#F8FAFC"))
    };

    private void OnWindowOpened(object? sender, EventArgs e)
    {
        PositionAtBottomRight();
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
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
        var pixelWidth = (int)Math.Round(Width * scaling);
        var pixelHeight = (int)Math.Round(Height * scaling);
        var pixelMargin = (int)Math.Round(ScreenMarginPx * scaling);

        var offset = OverlayPositionCalculator.CalculateBottomRight(
            workArea.Width,
            workArea.Height,
            pixelWidth,
            pixelHeight,
            pixelMargin);

        Position = new PixelPoint(workArea.X + offset.X, workArea.Y + offset.Y);
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        Dispatcher.UIThread.Post(ApplyViewModelState);
    }

    private void ApplyViewModelState()
    {
        _batteryTextBlock.Text = _viewModel.BatteryText;
        _solarTextBlock.Text = _viewModel.SolarText;
        _houseTextBlock.Text = _viewModel.HouseText;
        _generatorTextBlock.Text = _viewModel.GeneratorText;
        _statusTextBlock.Text = _viewModel.StatusMessage;
        _statusTextBlock.Foreground = new SolidColorBrush(
            Color.Parse(_viewModel.HasError ? "#F87171" : "#94A3B8"));
    }
}

