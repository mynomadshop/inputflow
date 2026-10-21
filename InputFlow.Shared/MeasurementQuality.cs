using System;

namespace InputFlow.Shared;

/// <summary>Indicates the confidence level of distance measurements.</summary>
public enum MeasurementQuality
{
    /// <summary>Fully calibrated, high-confidence measurements.</summary>
    Calibrated,

    /// <summary>Based on unverified physical-size estimates from hardware data.</summary>
    Estimated,

    /// <summary>Only pixel data available (no calibration applied).</summary>
    PixelsOnly,

    /// <summary>Data temporarily unavailable (e.g., during topology change or sleep).</summary>
    Unavailable
}
