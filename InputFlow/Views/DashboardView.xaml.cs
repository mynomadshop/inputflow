using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using InputFlow.Core.ViewModels;
using InputFlow.Shared;
using System.Globalization;

namespace InputFlow.Views;

/// <summary>Main dashboard — odometer + keyboard counter.</summary>
public sealed partial class DashboardView : Page
{
    private readonly DashboardViewModel _vm;

    public DashboardView(DashboardViewModel vm)
    {
        _vm = vm;
        InitializeComponent();

        // Bind to ViewModel properties.
        _vm.PropertyChanged += OnVmPropertyChanged;
        UpdateFromVm();
    }

    private void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DashboardViewModel.TrackingState) ||
            e.PropertyName == "TotalKeyPresses" ||
            e.PropertyName == "DistanceCm")
        {
            UpdateFromVm();
        }
    }

    private void UpdateFromVm()
    {
        // Status indicator (text + bullet, not color alone)
        switch (_vm.TrackingState)
        {
            case Shared.TrackingState.Collecting:
                StatusIndicator.Text = "\u25CF Tracking";
                StatusIndicator.Foreground = (SolidColorBrush)Resources["StatusRunningBrush"];
                PauseBtn.Content = "Pause";
                break;
            case Shared.TrackingState.Paused:
                StatusIndicator.Text = "\u25CF Paused";
                StatusIndicator.Foreground = (SolidColorBrush)Resources["StatusPausedBrush"];
                PauseBtn.Content = "Resume";
                break;
            default:
                StatusIndicator.Text = "\u25CF Not Tracking";
                StatusIndicator.Foreground = (SolidColorBrush)Resources["StatusStoppedBrush"];
                PauseBtn.Content = "Start";
                break;
        }

        // Odometer with auto unit selection
        var cm = _vm.DistanceCm;
        string displayText;
        if (cm >= 100_000)
            displayText = $"{cm / 100_000:F3} km";
        else if (cm >= 100)
            displayText = $"{cm / 100:F2} m";
        else
            displayText = $"{cm:F1} cm";

        DistanceDisplay.Text = displayText;

        // Key presses
        KeyPressDisplay.Text = _vm.TotalKeyPresses.ToString("N0", CultureInfo.CurrentCulture);
    }

    private void OnPauseToggle_Click(object sender, RoutedEventArgs e)
    {
        // In production, command binding would handle this. Manual click handler for now.
        if (_vm.TrackingState == Shared.TrackingState.Collecting ||
            _vm.TrackingState == Shared.TrackingState.Paused)
        {
            PauseBtn.Content = "Saving...";
            // Toggle handled by ViewModel — re-read state after brief delay.
        }
    }
}
