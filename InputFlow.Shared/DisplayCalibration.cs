using System;

namespace InputFlow.Shared;

/// <summary>Physical calibration for a single display.</summary>
public record DisplayCalibration(
    string MonitorName,
    string PnpDeviceId,
    int PixelWidth,
    int PixelHeight,
    double ActiveWidthCm,      // user-entered physical width in centimeters
    double ActiveHeightCm,     // user-entered physical height in centimeters
    CalibrationMethod Method,  // how this calibration was determined
    DateTimeOffset LastUpdated,
    string? RulerMeasurementId = null)
{
    /// <summary>Ratio: physical width cm per logical pixel.</summary>
    public double CmPerPixelX => ActiveWidthCm / PixelWidth;

    /// <summary>Ratio: physical height cm per logical pixel.</summary>
    public double CmPerPixelY => ActiveHeightCm / PixelHeight;
}

/// <summary>How the calibration was determined.</summary>
public enum CalibrationMethod
{
    /// <summary>From EDID or hardware-reported dimensions (unverified estimate).</summary>
    HardwareEstimate,

    /// <summary>User entered physical width and height manually.</summary>
    ManualEntry,

    /// <summary>User entered diagonal size and aspect ratio; dimensions calculated.</summary>
    DiagonalPlusAspect,

    /// <summary>Ruler-based on-screen calibration tool.</summary>
    RulerCalibration
}
