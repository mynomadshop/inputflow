using System;

namespace InputFlow.Shared;

/// <summary>Daily summary of aggregated input metrics.</summary>
public record DailySummary(
    DateOnly Date,
    int TotalKeyPresses,
    double? TotalDistanceCm,
    MeasurementQuality DistanceQuality,
    string SourceDisplayIds);  // comma-separated PNP device IDs for provenance
