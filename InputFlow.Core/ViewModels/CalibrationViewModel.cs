using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using InputFlow.Core.Measurement;
using InputFlow.Shared;

namespace InputFlow.Core.ViewModels;

/// <summary>View model for the Calibration page.</summary>
public partial class CalibrationViewModel : ObservableObject
{
    private readonly DisplayTopology _topology;

    [ObservableProperty]
    private ObservableCollection<CalibrationEntry> _entries = new();

    public CalibrationViewModel(DisplayTopology topology)
    {
        _topology = topology;
        RefreshDisplays();
    }

    public void RefreshDisplays()
    {
        Entries.Clear();
        foreach (var display in _topology.Displays)
        {
            var cal = _topology.GetEmptyCalibration(display);
            Entries.Add(new CalibrationEntry(display, cal));
        }
    }

    public async Task SaveAllAsync()
    {
        foreach (var entry in Entries)
        {
            var cal = new DisplayCalibration(
                MonitorName: entry.DisplayName,
                PnpDeviceId: entry.PnpDeviceId,
                PixelWidth: entry.PixelWidth,
                PixelHeight: entry.PixelHeight,
                ActiveWidthCm: entry.WidthCm,
                ActiveHeightCm: entry.HeightCm,
                Method: CalibrationMethod.ManualEntry,
                LastUpdated: DateTimeOffset.UtcNow);

            _topology.SaveCalibration(entry.PnpDeviceId, cal);
        }
    }
}

/// <summary>A single display calibration row in the UI.</summary>
public partial class CalibrationEntry : ObservableObject
{
    private readonly DisplayInfo _display;
    private readonly DisplayCalibration _cal;

    public string DisplayName => _display.Name;
    public string PnpDeviceId => _display.PnpDeviceId;
    public int PixelWidth => _display.PixelWidth;
    public int PixelHeight => _display.PixelHeight;
    public string Resolution => $"{PixelWidth}\u00D7{PixelHeight}";

    [ObservableProperty] private double _widthCm;
    [ObservableProperty] private double _heightCm;

    public CalibrationEntry(DisplayInfo display, DisplayCalibration cal)
    {
        _display = display;
        _cal = cal;
        WidthCm = cal.ActiveWidthCm;
        HeightCm = cal.ActiveHeightCm;
    }
}
