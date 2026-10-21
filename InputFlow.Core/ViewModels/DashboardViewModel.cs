using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InputFlow.Core.Aggregation;
using InputFlow.Shared;

namespace InputFlow.Core.ViewModels;

/// <summary>View model for the Dashboard — main statistics display.</summary>
public partial class DashboardViewModel : ObservableObject
{
    private readonly SessionManager _session;

    [ObservableProperty] private string? _unitDisplay = "Auto";
    [ObservableProperty] private TrackingState _trackingState = TrackingState.Stopped;
    [ObservableProperty] private string _statusText = "Not Tracking";
    [ObservableProperty] private string _statusColor = "#FF5252";  // Red for stopped

    public DashboardViewModel(SessionManager session)
    {
        _session = session;
        _session.PropertyChanged += OnSessionPropertyChanged;
        
        if (_session.State == TrackingState.Collecting)
        {
            UpdateStatus(true);
        }
    }

    private void OnSessionPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Sync tracking state from SessionManager.
        if (e.PropertyName == nameof(SessionManager.State))
        {
            TrackingState = _session.State;
            UpdateStatus(_session.State == TrackingState.Collecting);
            OnPropertyChanged(nameof(TrackingState));
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(StatusColor));
        }
    }

    private void UpdateStatus(bool collecting)
    {
        if (collecting)
        {
            StatusText = "\u25CF Tracking";  // Green bullet
            StatusColor = "#4ADE80";  // Green
        }
        else if (_session.State == TrackingState.Paused)
        {
            StatusText = "\u25CF Paused";
            StatusColor = "#FBBF24";  // Yellow
        }
        else
        {
            StatusText = "\u25CF Not Tracking";
            StatusColor = "#FF5252";  // Red
        }
    }

    [RelayCommand]
    private async Task ToggleTrackingAsync()
    {
        if (_session.State == TrackingState.Stopped || _session.State == TrackingState.Paused)
        {
            if (_session.State == TrackingState.Paused)
                _session.ResumeTracking();
            else
                _session.StartTracking();
        }
        else
        {
            _session.PauseTracking();
        }
    }

    [RelayCommand]
    private async Task StopAndResetAsync()
    {
        _session.StopTracking();
    }

    [RelayCommand]
    private void RefreshNow()
    {
        _session.RefreshNow();
    }
}
