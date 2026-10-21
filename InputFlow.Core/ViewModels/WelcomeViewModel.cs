using System;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace InputFlow.Core.ViewModels;

/// <summary>View model for the Welcome / Onboarding screen.</summary>
public partial class WelcomeViewModel : ObservableObject
{
    [ObservableProperty] private bool _keyboardTrackingEnabled = true;
    [ObservableProperty] private bool _pointerTrackingEnabled = true;
    [ObservableProperty] private bool _isConsentGiven;

    [RelayCommand]
    private async Task AcceptConsentAsync()
    {
        IsConsentGiven = true;
        // In production: persist consent to settings store.
    }

    [RelayCommand]
    private async Task DeclineAndCloseAsync()
    {
        // Close the application.
    }
}
