using System;

namespace InputFlow.Shared;

/// <summary>Aggregate pointer/cursor metrics for a reporting period.</summary>
public record PointerMetrics(
    double DistanceCm,
    double DistanceM => DistanceCm / 100.0,
    double DistanceKm => DistanceCm / 100_000.0,
    MeasurementQuality Quality,
    int LastPixelX,
    int LastPixelY,
    DateTime Timestamp);
