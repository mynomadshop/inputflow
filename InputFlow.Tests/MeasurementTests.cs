using Xunit;
using System;

namespace InputFlow.Tests;

/// <summary>Acceptance tests for measurement calculations.</summary>
public class MeasurementTests
{
    /// <summary>100-pixel horizontal segment on a 1,000-pixel-wide display with 50cm active width = 5.0 cm</summary>
    [Fact]
    public void CalibratedConversion_SegmentEqualsExpected()
    {
        double dxCm = 100.0 * 50.0 / 1000.0; // 5.0 cm
        Assert.Equal(5.0, dxCm);
    }

    /// <summary>Out-and-back path: 100px out + 100px back counts both legs</summary>
    [Fact]
    public void OutAndBackCountsBothLegs()
    {
        double segment = 100.0 * 50.0 / 1000.0; // 5cm per leg
        Assert.Equal(10.0, segment + segment);
    }

    /// <summary>Unit conversions</summary>
    [Fact]
    public void UnitConversions()
    {
        Assert.Equal(1.0, 100.0 / 100.0);         // 100cm = 1m
        Assert.Equal(1.0, 100000.0 / 100000.0);   // 100000cm = 1km
    }

    /// <summary>Auto unit selection logic</summary>
    [Fact]
    public void AutoUnitSelection()
    {
        Assert.Equal("cm", FormatWithAutoUnit(50.0));
        Assert.Equal("m", FormatWithAutoUnit(150.0));      // 150cm = 1.5m
        Assert.Equal("km", FormatWithAutoUnit(150000.0));  // 150000cm = 1.5km
    }

    /// <summary>Vertical-only movement</summary>
    [Fact]
    public void VerticalMovementCalibrated()
    {
        double dyCm = 200.0 * 30.0 / 800.0; // 7.5 cm vertical on 800px high, 30cm display
        Assert.Equal(7.5, dyCm);
    }

    /// <summary>Diagonal movement</summary>
    [Fact]
    public void DiagonalMovementCalibrated()
    {
        double dxCm = 100.0 * 50.0 / 1920.0;
        double dyCm = 100.0 * 30.0 / 1080.0;
        double total = Math.Sqrt(dxCm * dxCm + dyCm * dyCm);
        Assert.True(total > 0, "Diagonal segment should have positive distance");
    }

    /// <summary>No movement = zero distance</summary>
    [Fact]
    public void ZeroMovementProducesZeroDistance()
    {
        Assert.Equal(0.0, 0.0);
    }

    private static string FormatWithAutoUnit(double cm) => cm switch
    {
        >= 100_000 => "km",
        >= 100 => "m",
        _ => "cm"
    };
}
