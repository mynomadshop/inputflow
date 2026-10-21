using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using InputFlow.Views;

namespace InputFlow;

/// <summary>Main application window with navigation.</summary>
public sealed partial class MainWindow : Window
{
    private readonly App _app;
    private readonly string[] _routes = { "dashboard", "history", "calibration", "privacy", "settings" };
    private readonly Page[] _pages = new Page[5];

    public MainWindow(App app)
    {
        _app = app;
        InitializeComponent();

        // Pre-create all pages (simpler than lazy-loading for now).
        var dashboard = new DashboardPage(_app.DashboardVm!, _app.SettingsVm);
        var history = new HistoryPage();
        var calibration = new CalibrationPage();
        var privacy = new PrivacyPage();
        var settings = new SettingsPage(_app.SettingsVm);

        // Register content loaded events for navigation.
        NavView.SelectionChanged += OnNavSelectionChanged;

        // Default to dashboard.
        NavigateTo(0, dashboard);
    }

    private void OnNavSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            ContentFrame.Navigate(typeof(SettingsPage));
            return;
        }

        var item = args.SelectedItem as NavigationViewItem;
        if (item?.Tag is string tag)
        {
            int index = System.Array.IndexOf(_routes, tag);
            if (index >= 0)
            {
                // Navigate based on tag name.
                switch (tag)
                {
                    case "dashboard":
                        ContentFrame.Navigate(typeof(DashboardPage), _app.DashboardVm);
                        break;
                    case "history":
                        ContentFrame.Navigate(typeof(HistoryPage));
                        break;
                    case "calibration":
                        ContentFrame.Navigate(typeof(CalibrationPage));
                        break;
                    case "privacy":
                        ContentFrame.Navigate(typeof(PrivacyPage));
                        break;
                }
            }
        }
    }

    private void NavigateTo(int index, Page page)
    {
        // Navigation handled via Frame.Navigate.
    }

    /// <summary>Handle window close — respect CloseBehavior setting.</summary>
    protected override void OnClosed(WindowEventArgs args)
    {
        if (_app.SettingsVm.CloseBehavior == Shared.CloseBehavior.MinimizeToTray)
        {
            args.Handled = true;
            this.Hide();
            // Show tray icon with Open/Pause/Quit menu.
        }
        // else: let the window close naturally (default behavior).
    }
}
