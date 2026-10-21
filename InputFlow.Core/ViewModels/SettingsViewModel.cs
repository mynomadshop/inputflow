using System;
using CommunityToolkit.Mvvm.ComponentModel;
using InputFlow.Shared;

namespace InputFlow.Core.ViewModels;

/// <summary>View model for Settings page.</summary>
public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty] private CursorDisplayUnit _distanceUnit = CursorDisplayUnit.Auto;
    [ObservableProperty] private string _theme = "dark";
    [ObservableProperty] private bool _animationsEnabled = true;
    [ObservableProperty] private bool _startWithWindows = false;
    [ObservableProperty] private CloseBehavior _closeBehavior = CloseBehavior.QuitImmediately;

    public void ApplySettings(AppSettings settings)
    {
        DistanceUnit = settings.DistanceUnit;
        Theme = settings.Theme;
        AnimationsEnabled = settings.AnimationsEnabled;
        StartWithWindows = settings.StartWithWindows;
        CloseBehavior = settings.CloseBehavior;
    }

    public AppSettings ToSettings() => new(
        DistanceUnit: DistanceUnit,
        KeyboardTrackingEnabled: false,  // managed by session
        PointerTrackingEnabled: false,   // managed by session
        Theme: Theme,
        AnimationsEnabled: AnimationsEnabled,
        StartWithWindows: StartWithWindows,
        CloseBehavior: CloseBehavior,
        RetentionDays30SecBuckets: 7,
        RetentionMonthsDailySummaries: 12,
        ConsentLevel: PrivacyConsentLevel.Given,
        LastConsentUpdate: DateTimeOffset.UtcNow);
}
