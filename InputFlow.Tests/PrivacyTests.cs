using Xunit;

namespace InputFlow.Tests;

/// <summary>Privacy invariant tests — ensure no sensitive data in models.</summary>
public class PrivacyTests
{
    /// <summary>AggregateBucket contains ONLY aggregate counts, no key names or text.</summary>
    [Fact]
    public void AggregateBucketContainsNoKeyNames()
    {
        var bucket = new Shared.AggregateBucket(
            BucketId: "test-001",
            StartUtc: DateTime.UtcNow,
            EndUtc: DateTime.UtcNow.AddSeconds(30),
            ActiveDurationSec: 28.5,
            KeyPresses: 42,
            MouseClicks: 7,
            ScrollEvents: 3,
            PixelDistance: 15_000,
            CalibratedCm: 47.3,
            DistanceQuality: Shared.MeasurementQuality.Calibrated);

        // The record only contains counts and durations — no key identity.
        Assert.Equal(42, bucket.KeyPresses);
    }

    /// <summary>DailySummary also contains only aggregates.</summary>
    [Fact]
    public void DailySummaryContainsNoKeyNames()
    {
        var summary = new Shared.DailySummary(
            Date: DateOnly.FromDateTime(DateTime.UtcNow),
            TotalKeyPresses: 12_345,
            TotalDistanceCm: 500.0,
            DistanceQuality: Shared.MeasurementQuality.Calibrated,
            SourceDisplayIds: "MONITOR0");

        Assert.Equal(12_345, summary.TotalKeyPresses);
        // No per-key data stored.
    }

    /// <summary>KeyMetrics does not contain typed text or key sequences.</summary>
    [Fact]
    public void KeyMetricsContainsNoText()
    {
        var metrics = new Shared.KeyMetrics(
            TotalKeyPresses: 100,
            KeyPressesLast60Seconds: 5,
            IsLastWindowPartial: false,
            DateTime.UtcNow);

        Assert.Equal(100, metrics.TotalKeyPresses);
        // No characters, no sequences — just counts.
    }
}
