using System;
using CommunityToolkit.Mvvm.ComponentModel;
using InputFlow.Shared;

namespace InputFlow.Core.Capture;

/// <summary>
/// Captures pointer movement and accumulates cursor travel distance.
/// 
/// PRIVACY DESIGN:
///   - Only accumulates distances between consecutive positions.
///   - Never stores coordinate trails, replayable paths, or per-event positions.
///   - After each segment calculation, only the aggregate totals are retained.
///   - Pixel distance is stored as an uncalibrated provenance number; cm values come from calibration.
/// 
/// MEASUREMENT:
///   For two consecutive cursor positions (x1,y1) and (x2,y2) on display D:
///     dx_cm = (x2-x1) * D.widthCm / D.pixelWidth
///     dy_cm = (y2-y1) * D.heightCm / D.pixelHeight  
///     segment_cm = sqrt(dx^2 + dy^2)
///   Sum every valid segment. Out-and-back paths count both legs.
/// </summary>
public partial class PointerCapturer : ObservableObject, IDisposable
{
    private double _totalCm;
    private long _pixelDistance;  // Manhattan distance in pixels (provenance)
    private int? _lastX = null;
    private int? _lastY = null;

    [ObservableProperty]
    private double _distanceCm;

    [ObservableProperty]
    private double _distanceM;

    [ObservableProperty]
    private double _distanceKm;

    [ObservableProperty]
    private MeasurementQuality _quality = MeasurementQuality.PixelsOnly;

    /// <summary>Process a movement segment between two consecutive cursor positions.</summary>
    public void RecordSegment(
        int x1, int y1, 
        int x2, int y2,
        DisplayCalibration calibration)
    {
        if (_lastX == null || _lastY == null)
        {
            // First position — store and wait for the next.
            _lastX = x2;
            _lastY = y2;
            return;
        }

        if (calibration != null && calibration.ActiveWidthCm > 0 && calibration.ActiveHeightCm > 0)
        {
            // Apply calibrated conversion per display formula.
            double dxCm = (x2 - x1) * calibration.CmPerPixelX;
            double dyCm = (y2 - y1) * calibration.CmPerPixelY;
            double segment = Math.Sqrt(dxCm * dxCm + dyCm * dyCm);

            _totalCm += segment;
            _pixelDistance += Math.Abs(x2 - x1) + Math.Abs(y2 - y1);
            UpdateQuality(calibration.Method);
        }
        else
        {
            // No calibration — track pixel distance only.
            _pixelDistance += Math.Abs(x2 - x1) + Math.Abs(y2 - y1);
        }

        _lastX = x2;
        _lastY = y2;

        OnPropertyChanged(nameof(DistanceCm));
        OnPropertyChanged(nameof(DistanceM));
        OnPropertyChanged(nameof(DistanceKm));
    }

    /// <summary>Reset state (on pause, sleep-wake, display topology change).</summary>
    public void Reset()
    {
        _lastX = null;
        _lastY = null;
        // DO NOT reset totals — those are cumulative for the session.
    }

    /// <summary>Hard reset (new session).</summary>
    public void HardReset()
    {
        _totalCm = 0;
        _pixelDistance = 0;
        _lastX = null;
        _lastY = null;
        DistanceCm = 0;
        DistanceM = 0;
        DistanceKm = 0;
    }

    private void UpdateQuality(CalibrationMethod method)
    {
        Quality = method switch
        {
            CalibrationMethod.ManualEntry or CalibrationMethod.RulerCalibration
                => MeasurementQuality.Calibrated,
            CalibrationMethod.DiagonalPlusAspect
                => MeasurementQuality.Estimated,
            _ => MeasurementQuality.PixelsOnly
        };
    }

    public double TotalCm => _totalCm;
    public long PixelDistance => _pixelDistance;

    public void Dispose() { /* nothing to dispose */ }
}
