using System;

namespace InputFlow.Shared;

/// <summary>A 30-second aggregate bucket of raw input events.</summary>
public record AggregateBucket(
    string BucketId,                    // ISO timestamp or GUID for deduplication
    DateTime StartUtc,
    DateTime EndUtc,
    double ActiveDurationSec,           // actual tracking time (excludes pauses/sleep)
    int KeyPresses,                     // total initial key-down transitions
    int MouseClicks,                    // left + right + middle button presses
    int ScrollEvents,                   // vertical + horizontal scroll events
    long PixelDistance,                 // uncalibrated pixel-distance sum (provenance)
    double? CalibratedCm,               // calibrated distance in cm (null if uncalibrated)
    MeasurementQuality DistanceQuality); // calibration quality level
