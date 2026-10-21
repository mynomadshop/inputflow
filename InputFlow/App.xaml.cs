using Microsoft.UI.Xaml;
using InputFlow.Core.Aggregation;
using InputFlow.Core.Capture;
using InputFlow.Core.Measurement;
using InputFlow.Core.Storage;
using System;

namespace InputFlow;

/// <summary>Application lifecycle: initializes services, creates main window.</summary>
public partial class App : Application
{
    private readonly RawInputCapturer _capturer = new();
    private readonly PointerCapturer _pointer = new();
    private AggregateStore? _store;
    private DisplayTopology? _topology;
    private SessionManager? _session;

    public SettingsViewModel SettingsVm { get; } = new();
    public DashboardViewModel? DashboardVm { get; private set; }
    public Window MainWindowInstance { get; private set; } = null!;

    public App()
    {
        UnhandledException += OnUnhandledException;
    }

    private void OnNavigated(object sender, WindowEventArgs args)
    {
        // Navigation will be handled in MainWindow.
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        var dataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "InputFlow");
        Directory.CreateDirectory(dataDir);

        _store = new AggregateStore(Path.Combine(dataDir, "data.db"));
        _topology = new DisplayTopology();
        _topology.RefreshDisplays();

        var settings = new SettingsStore(Path.Combine(dataDir, "data.db")).Load();
        SettingsVm.ApplySettings(settings);

        _session = new SessionManager(_capturer, _pointer, _store!, _topology!);
        DashboardVm = new DashboardViewModel(_session);

        MainWindowInstance = new MainWindow(this);
        MainWindowInstance.Activate();
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        // Log and show error — don't crash silently.
        System.Diagnostics.Debug.WriteLine($"UNHANDLED: {e.Exception}");
    }
}
