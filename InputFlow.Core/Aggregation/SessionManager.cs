using System;
using System.Timers;
using CommunityToolkit.Mvvm.ComponentModel;
using InputFlow.Core.Capture;
using InputFlow.Core.Measurement;
using InputFlow.Core.Storage;
using InputFlow.Shared;

namespace InputFlow.Core.Aggregation;

/// <summary>
/// Manages a tracking session with 30-second reporting cadence.
/// 
/// RESPONSIBILITIES:
///   - Coordinates the RawInputCapturer and PointerCapturer.
///   - Flushes 30-second aggregate buckets to persistent storage.
///   - Publishes updated metrics to the UI layer.
///   - Handles lifecycle: start, pause, resume, stop, sleep/lock/wake.
/// 
/// DESIGN:
///   Collection is continuous (while collecting + enabled).
///   Reporting (persisting aggregates and notifying UI) happens every 30 seconds.
///   These are separate concerns: the capturer never blocks on disk I/O.
/// </summary>
public partial class SessionManager : ObservableObject, IDisposable
{
    private readonly RawInputCapturer _capturer;
    private readonly PointerCapturer _pointer;
    private readonly AggregateStore _store;
    private readonly DisplayTopology _topology;

    // 30-second reporting timer (system.Timers for independence from UI thread).
    private Timer? _reportTimer;
    private DateTime _bucketStart;
    private int _startKeyPressesAtBucket;
    private int _startClicksAtBucket;
    private int _startScrollsAtBucket;
    private double _pointerCmAtBucketStart;

    // Current accumulated values (pushed to UI every 30 seconds)
    [ObservableProperty]
    private TrackingState _state = TrackingState.Stopped;

    [ObservableProperty]
    private double _distanceCm;

    [ObservableProperty]
    private double _distanceM;

    [ObservableProperty]
    private double _distanceKm;

    [ObservableProperty]
    private int _totalKeyPresses;

    [ObservableProperty]
    private MeasurementQuality _quality = MeasurementQuality.Unavailable;

    [ObservableProperty]
    private string? _lastUpdated;

    // Events for explicit start/stop control.
    public event EventHandler<bool>? TrackingStarted;   // bool = backgroundEnabled
    public event EventHandler? TrackingStopped;

    public SessionManager(
        RawInputCapturer capturer,
        PointerCapturer pointer,
        AggregateStore store,
        DisplayTopology topology)
    {
        _capturer = capturer;
        _pointer = pointer;
        _store = store;
        _topology = topology;

        // Wire capturer events to pointer recorder.
        _capturer.PointerMoved += (_, e) => OnPointerMove();

        _logger?.Information("SessionManager constructed");
    }

    // -----------------------------------------------------------------------
    // Public lifecycle
    // -----------------------------------------------------------------------

    /// <summary>Begin tracking — starts capturing and 30-second reporting.</summary>
    public void StartTracking(bool backgroundEnabled = false)
    {
        if (_state == TrackingState.Collecting) return;

        // Initialize capturer.
        _capturer.Start();
        _capturer.Resume();

        // Reset measurement baseline.
        _pointer.Reset();

        // Flush the previous partial bucket (if any).
        FlushCurrentBucket();

        // Start the 30-second reporting timer.
        _bucketStart = DateTime.UtcNow;
        _startKeyPressesAtBucket = _capturer.CurrentTotalKeyPresses;
        _pointerCmAtBucketStart = _pointer.TotalCm;

        _reportTimer = new Timer(30_000) { AutoReset = true };
        _reportTimer.Elapsed += OnReportTick;
        _reportTimer.Start();

        _state = TrackingState.Collecting;
        UpdateFromCapturer();
        
        TrackingStarted?.Invoke(this, backgroundEnabled);
        _logger?.Information("Session started");
    }

    /// <summary>Pause tracking — freezes collection immediately.</summary>
    public void PauseTracking()
    {
        if (_state != TrackingState.Collecting) return;

        // Flush current bucket before pausing.
        FlushCurrentBucket();

        _capturer.Pause();
        _reportTimer?.Stop();
        _state = TrackingState.Paused;
        LastUpdated = DateTime.UtcNow.ToString("HH:mm:ss");
        
        _logger?.Information("Session paused");
    }

    /// <summary>Resume tracking — restores collection with rebaseline.</summary>
    public void ResumeTracking()
    {
        if (_state != TrackingState.Paused) return;

        _capturer.Resume();
        _pointer.Reset();

        _bucketStart = DateTime.UtcNow;
        _startKeyPressesAtBucket = _capturer.CurrentTotalKeyPresses;
        _pointerCmAtBucketStart = _pointer.TotalCm;

        _reportTimer?.Start();
        _state = TrackingState.Collecting;
        UpdateFromCapturer();

        _logger?.Information("Session resumed");
    }

    /// <summary>Stop tracking — stops all collection and persists data.</summary>
    public void StopTracking()
    {
        FlushCurrentBucket();
        _capturer.Stop();
        _reportTimer?.Stop();
        _reportTimer?.Dispose();
        _reportTimer = null;

        _state = TrackingState.Stopped;
        TrackingStopped?.Invoke(this, EventArgs.Empty);
        LastUpdated = DateTime.UtcNow.ToString("HH:mm:ss");

        _logger?.Information("Session stopped");
    }

    /// <summary>Force a refresh of the dashboard without resetting counters.</summary>
    public void RefreshNow()
    {
        FlushCurrentBucket();
        UpdateFromCapturer();
    }

    // -----------------------------------------------------------------------
    // Internal operations
    // -----------------------------------------------------------------------

    /// <summary>Handle pointer movement — update distance in real-time for live display.</summary>
    private void OnPointerMove()
    {
        if (_state != TrackingState.Collecting) return;

        // In production, we'd capture actual cursor positions from Win32 GetCursorPos / 
        // GetPhysicalCursorPos. Here the pointer capturer updates its own totals.
        
        UpdateFromCapturer();
    }

    /// <summary>30-second tick: flush bucket to storage and update UI.</summary>
    private void OnReportTick(object? sender, ElapsedEventArgs e)
    {
        try
        {
            FlushCurrentBucket();
            UpdateFromCapturer();
        }
        catch (Exception ex)
        {
            _logger?.Error(ex, "Error flushing bucket");
        }
    }

    /// <summary>Finalize the current partial bucket and save to store.</summary>
    private void FlushCurrentBucket()
    {
        var now = DateTime.UtcNow;
        var activeDuration = (now - _bucketStart).TotalSeconds;

        var keyPressesThisBucket = 
            _capturer.CurrentTotalKeyPresses - _startKeyPressesAtBucket;

        // Ensure non-negative (handles race conditions on pause/resume).
        if (keyPressesThisBucket < 0) keyPressesThisBucket = 0;

        double distanceCmThisBucket = Math.Max(0, _pointer.TotalCm - _pointerCmAtBucketStart);

        var bucketId = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss-fff");

        var bucket = new AggregateBucket(
            BucketId: bucketId,
            StartUtc: _bucketStart,
            EndUtc: now,
            ActiveDurationSec: activeDuration,
            KeyPresses: keyPressesThisBucket,
            MouseClicks: Math.Max(0, _capturer.MouseClickCount - (_startClicksAtBucket)),
            ScrollEvents: Math.Max(0, _capturer.ScrollEventCount - (_startScrollsAtBucket)),
            PixelDistance: _pointer.PixelDistance,
            CalibratedCm: distanceCmThisBucket > 0 ? distanceCmThisBucket : (double?)null,
            DistanceQuality: _pointer.Quality);

        try
        {
            _store.SaveBucketAsync(bucket).Wait();
        }
        catch (Exception ex)
        {
            _logger?.Error(ex, "Failed to save bucket {Id}", bucketId);
        }

        // Reset per-bucket tracking.
        _bucketStart = now;
        _startKeyPressesAtBucket = _capturer.CurrentTotalKeyPresses;
        _pointerCmAtBucketStart = _pointer.TotalCm;
    }

    /// <summary>Pull current values from capturers and push to UI properties.</summary>
    private void UpdateFromCapturer()
    {
        TotalKeyPresses = _capturer.CurrentTotalKeyPresses;
        DistanceCm = _pointer.TotalCm;
        DistanceM = _pointer.TotalCm / 100.0;
        DistanceKm = _pointer.TotalCm / 100_000.0;
        Quality = _pointer.Quality;
        LastUpdated = DateTime.UtcNow.ToString("HH:mm:ss");
    }

    public void Dispose()
    {
        StopTracking();
        GC.SuppressFinalize(this);
    }

    private static readonly Serilog.ILogger _logger =
        Serilog.Log.ForContext<SessionManager>();
}
