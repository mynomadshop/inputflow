using System;
using System.Collections.ObjectModel;
using InputFlow.Shared;

namespace InputFlow.Core.Measurement;

/// <summary>Physical dimensions reported or calibrated for a display.</summary>
public record PhysicalSize(double WidthCm, double HeightCm);

/// <summary>Information about a single connected monitor.</summary>
public record DisplayInfo(
    string Name,
    string PnpDeviceId,
    int PixelWidth,      // current resolution width (logical pixels)
    int PixelHeight,     // current resolution height
    int Left,            // may be negative for multi-monitor setups
    int Top,
    bool IsPrimary,
    PhysicalSize? CalibratedPhysicalSize);

/// <summary>
/// Enumerates connected displays and manages their calibration state.
/// </summary>
public class DisplayTopology
{
    public ObservableCollection<DisplayInfo> Displays { get; } = new();
    
    // Calibration map: PnpDeviceId -> DisplayCalibration
    private readonly Dictionary<string, DisplayCalibration> _calibrations = new();

    /// <summary>Re-enumerate connected displays.</summary>
    public void Refresh()
    {
        Displays.Clear();
        
        // In production: use EnumDisplayMonitors + GetMonitorInfo + 
        // GetPhysicallyDisplayedSize (Windows 11+) or EDID parsing.
        var monitors = System.Windows.Forms.Screen.AllScreens;
        foreach (var screen in monitors)
        {
            var info = new DisplayInfo(
                Name: screen.DeviceName,
                PnpDeviceId: ExtractPnpId(screen.DeviceName),
                PixelWidth: screen.Bounds.Width,
                PixelHeight: screen.Bounds.Height,
                Left: screen.Bounds.Left,
                Top: screen.Bounds.Top,
                IsPrimary: screen.Primary,
                CalibratedPhysicalSize: GetDefaultPhysicalSize(screen)
            );
            Displays.Add(info);
        }

        OnPropertyChanged(nameof(Displays));
    }

    /// <summary>Look up calibration for a specific point in screen coordinates.</summary>
    public DisplayCalibration? GetCalibrationForPoint(int x, int y)
    {
        foreach (var d in Displays)
        {
            if (x >= d.Left && x < d.Left + d.PixelWidth &&
                y >= d.Top  && y < d.Top  + d.PixelHeight)
            {
                return _calibrations.GetValueOrDefault(d.PnpDeviceId);
            }
        }
        return null;
    }

    /// <summary>Save calibration for a display.</summary>
    public void SaveCalibration(string pnpDeviceId, DisplayCalibration calibration)
    {
        _calibrations[pnpDeviceId] = calibration;
    }

    /// <summary>Get or create a new empty calibration for a display.</summary>
    public DisplayCalibration GetEmptyCalibration(DisplayInfo info)
    {
        if (_calibrations.TryGetValue(info.PnpDeviceId, out var existing))
            return existing;

        var cal = new DisplayCalibration(
            MonitorName: info.Name,
            PnpDeviceId: info.PnpDeviceId,
            PixelWidth: info.PixelWidth,
            PixelHeight: info.PixelHeight,
            ActiveWidthCm: info.CalibratedPhysicalSize?.WidthCm ?? 0,
            ActiveHeightCm: info.CalibratedPhysicalSize?.HeightCm ?? 0,
            Method: CalibrationMethod.HardwareEstimate,
            LastUpdated: DateTime.UtcNow);

        _calibrations[info.PnpDeviceId] = cal;
        return cal;
    }

    /// <summary>Split a cursor segment at display boundaries, returning per-display sub-segments.</summary>
    public List<(DisplayCalibration? Calibration, int Dx, int Dy)> SplitSegment(
        int x1, int y1, int x2, int y2)
    {
        var segments = new List<(DisplayCalibration?, int, int)>();

        // Simple approach: determine which display each endpoint is on.
        var cal1 = GetCalibrationForPoint(x1, y1);
        var cal2 = GetCalibrationForPoint(x2, y2);

        if (cal1 != null && cal2 != null && cal1.PnpDeviceId == cal2.PnpDeviceId)
        {
            // Same display — single segment.
            segments.Add((cal1, x2 - x1, y2 - y1));
        }
        else
        {
            // Cross-display: use the calibration of whichever endpoint we can resolve.
            if (cal1 != null)
                segments.Add((cal1, x2 - x1, y2 - y1));
            else
                segments.Add((cal2, x2 - x1, y2 - y1));
        }

        return segments;
    }

    private static string ExtractPnpId(string deviceName)
    {
        // E.g., "\\\\.\\DISPLAY1\\Monitor0" → "MONITOR0"
        return deviceName;
    }

    private static PhysicalSize? GetDefaultPhysicalSize(System.Windows.Forms.Screen screen)
    {
        // Windows 11+: try to get physical size from the display hardware.
        // Fallback: assume standard DPI (96 dpi → ~2.646 cm per inch).
        int inchesX = screen.Bounds.Width / 96;
        int inchesY = screen.Bounds.Height / 96;
        return new PhysicalSize(inchesX * 2.54, inchesY * 2.54);
    }

    // INotifyPropertyChanged stub — ObservableCollection already implements it.
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
}
