using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using InputFlow.Core.ViewModels;
using InputFlow.Shared;

namespace InputFlow.Views;

/// <summary>Welcome / onboarding page.</summary>
public sealed partial class WelcomeView : Page
{
    private readonly App _app;
    public event EventHandler? TrackingStarted;

    public WelcomeView(App app)
    {
        _app = app;
        InitializeComponent();
    }

    private void OnStartTracking_Click(object sender, RoutedEventArgs e)
    {
        // Persist settings from toggles.
        var kbEnabled = KeyboardToggle.IsOn;
        var ptEnabled = PointerToggle.IsOn;

        // Start the session manager.
        _app.SettingsVm.ApplySettings(
            new AppSettings(
                DistanceUnit: Shared.CursorDisplayUnit.Auto,
                KeyboardTrackingEnabled: kbEnabled,
                PointerTrackingEnabled: ptEnabled,
                Theme: "dark", AnimationsEnabled: true, 
                StartWithWindows: false, CloseBehavior: Shared.CloseBehavior.QuitImmediately,
                RetentionDays30SecBuckets: 7, RetentionMonthsDailySummaries: 12,
                ConsentLevel: Shared.PrivacyConsentLevel.Given,
                LastConsentUpdate: System.DateTimeOffset.UtcNow));

        // Navigate to dashboard.
        var main = Window.Current.Content as MainWindow;
        if (main != null)
        {
            main.NavView.SelectedItem = main.NavView.MenuItems[0];
        }

        TrackingStarted?.Invoke(this, EventArgs.Empty);
    }
}
