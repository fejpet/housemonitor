using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using HouseMonitor.Core.Presentation;

namespace HouseMonitor.App.Views;

/// <summary>
/// Modern dark-themed 2x2 HUD telemetry dashboard card control (REQ-024..REQ-035).
/// </summary>
public partial class HouseMonitorCard : UserControl
{
    public event EventHandler? CloseRequested;

    public HouseMonitorCard()
    {
        InitializeComponent();

        var closeButton = this.FindControl<Button>("CloseButton");
        if (closeButton is not null)
        {
            closeButton.Click += (_, _) => CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    public HouseMonitorCard(OverlayViewModel viewModel)
        : this()
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        DataContext = viewModel;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
