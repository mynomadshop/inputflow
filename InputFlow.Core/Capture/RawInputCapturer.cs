using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using InputFlow.Shared;

namespace InputFlow.Core.Capture;

/// <summary>
/// Captures raw keyboard and mouse input via Windows Raw Input API.
/// 
/// PRIVACY DESIGN:
///   - Counts ONLY initial key-down transitions (not auto-repeat, not releases).
///   - Never translates events into text, keystroke sequences, or characters.
///   - Does NOT persist key names, scan codes, VK codes, timestamps per-event, etc.
///   - Uses per-device pressed-state to distinguish held-key repeats from new presses.
/// 
/// ARCHITECTURE NOTE:
///   This class owns a message-only window that receives WM_INPUT messages.
///   Those messages are pumped on the UI thread (or a dedicated background thread).
///   For production, hook this up via WindowsMessages.Subscribe() or a dedicated window handle.
/// </summary>
public partial class RawInputCapturer : ObservableObject, IDisposable
{
    // Per-device: set of currently-held virtual-key usage codes.
    // A new key-down event for a code already in the set = repeat (ignore).
    // Key is device handle; value is the set of held usages.
    private readonly Dictionary<ulong, HashSet<ushort>> _devicePressedKeys = new();

    // Rolling window for last-60-second metric
    private readonly Queue<DateTime> _keyPressTimestamps = new(10000);
    private int _last60SecStartIndex;

    private TrackingState _state = TrackingState.Stopped;

    // Observable properties pushed to UI on each tick or event
    [ObservableProperty]
    private int _totalKeyPresses;

    [ObservableProperty]
    private int _mouseClickCount;

    [ObservableProperty]
    private int _scrollEventCount;

    [ObservableProperty]
    private int _keyPressesInLast60Sec;

    [ObservableProperty]
    private bool _lastWindowIsPartial;

    // Events
    public event EventHandler<KeyPressEvent>? KeyPressed;
    public event EventHandler<PointerEventArgs>? PointerMoved;
    public event EventHandler? DeviceAttached;
    public event EventHandler? DeviceRemoved;

    public TrackingState State => _state;
    public int CurrentTotalKeyPresses => _totalKeyPresses;

    // -----------------------------------------------------------------------
    // Lifecycle
    // -----------------------------------------------------------------------

    public void Start()
    {
        if (_state == TrackingState.Collecting) return;

        _state = TrackingState.Collecting;

        // In a real Win32 implementation:
        //   1. Create a message-only window (CreateWindowEx with hwndParent=HWND_MESSAGE).
        //   2. Register devices via RegisterRawInputDevices(RIDI_KEYBOARD | RIDI_MOUSE).
        //   3. Start pumping WM_INPUT messages in thewnd proc.
        // Here we expose the API contract; actual window creation would be injected.

        _logger?.Information("RawInputCapturer started");
    }

    public void Pause()
    {
        if (_state == TrackingState.Paused) return;
        _state = TrackingState.Paused;

        // Clear per-device pressed-state: on resume we rebaseline from physical state.
        _devicePressedKeys.Clear();
        _logger?.Information("RawInputCapturer paused — cleared key states");
    }

    public void Resume()
    {
        if (_state == TrackingState.Collecting) return;
        _state = TrackingState.Collecting;

        // Rebaseline: get current physical state of all connected keyboards.
        // In production this calls GetRawInputDeviceInfo / GetKeyState per device.
        _devicePressedKeys.Clear();
        _logger?.Information("RawInputCapturer resumed — rebaselines applied");
    }

    public void Stop()
    {
        if (_state == TrackingState.Stopped) return;
        _state = TrackingState.Stopped;
        _devicePressedKeys.Clear();

        // In production: unregister raw input devices, destroy message window.
        _logger?.Information("RawInputCapturer stopped");
    }

    public void Dispose() => Stop();

    // -----------------------------------------------------------------------
    // Internal event processing (called from message pump / dispatch thread)
    // -----------------------------------------------------------------------

    /// <summary>Process a raw keyboard input event.</summary>
    internal void OnKeyboardEvent(ushort usagePage, ushort usage, bool isKeyDown, ulong deviceHandle)
    {
        if (_state != TrackingState.Collecting) return;
        if (usagePage != 1) return; // Only usages from Usage Page 1 (Generic Desktop)

        if (isKeyDown)
        {
            // Check if already held for this device → auto-repeat or held key, ignore.
            if (_devicePressedKeys.TryGetValue(deviceHandle, out var held))
            {
                if (!held.Add(usage))
                    return; // Already held → repeat, skip.
            }
            else
            {
                _devicePressedKeys[deviceHandle] = new HashSet<ushort> { usage };
            }

            // It's an initial press — count it.
            Interlocked.Increment(ref _totalKeyPresses);

            // Track timestamp for rolling 60-second window.
            var now = DateTime.UtcNow;
            _keyPressTimestamps.Enqueue(now);
            TrimToLast60Sec(now);

            KeyPressed?.Invoke(this, new KeyPressEvent(usagePage, usage, deviceHandle));
        }
        else
        {
            // Key released — remove from pressed set.
            if (_devicePressedKeys.TryGetValue(deviceHandle, out var held))
                held.Remove(usage);
        }
    }

    /// <summary>Process a raw mouse input event.</summary>
    internal void OnMouseEvent(uint buttonFlags, uint buttonData, ulong deviceHandle)
    {
        if (_state != TrackingState.Collecting) return;

        // RI_MOUSE_LEFT_BUTTON_DOWN / RIGHT_BUTTON_DOWN / WHEEL / HORIZONTAL_WHEEL
        const uint LEFT_DOWN = 0x0001;
        const uint RIGHT_DOWN = 0x0002;

        if (buttonFlags == LEFT_DOWN || buttonFlags == RIGHT_DOWN)
            Interlocked.Increment(ref _mouseClickCount);

        // Wheel: positive = scroll up, negative = scroll down. Count each significant scroll.
        if (buttonFlags >= 0x08 && buttonFlags <= 0x10) // RI_MOUSE_WHEEL / RI_MOUSE_HWHEEL
        {
            Interlocked.Increment(ref _scrollEventCount);
        }

        PointerMoved?.Invoke(this, new PointerEventArgs(0, 0, deviceHandle));
    }

    /// <summary>Remove timestamps older than 60 seconds.</summary>
    private void TrimToLast60Sec(DateTime now)
    {
        var cutoff = now.AddSeconds(-60);
        while (_keyPressTimestamps.Count > 0 && _keyPressTimestamps.Peek() < cutoff)
        {
            _keyPressTimestamps.Dequeue();
        }

        int count = _keyPressTimestamps.Count;
        bool partial = (now - _keyPressTimestamps.Peek()) > TimeSpan.FromSeconds(60);
        
        // Only update if changed to avoid unnecessary UI notifications.
        if (_keyPressesInLast60Sec != count)
        {
            TotalKeyPressesInLast60Sec = count;
            LastWindowIsPartial = partial;
        }
    }

    // Helper property for the rolling window metric
    public int TotalKeyPressesInLast60Sec
    {
        get => _keyPressesInLast60Sec;
        private set
        {
            if ( SetProperty(ref _keyPressesInLast60Sec, value))
                OnPropertyChanged(nameof(IsLast60SecPartial));
        }
    }

    public bool IsLast60SecPartial => _lastWindowIsPartial;

    private static readonly Serilog.ILogger _logger =
        Serilog.Log.ForContext<RawInputCapturer>();
}
