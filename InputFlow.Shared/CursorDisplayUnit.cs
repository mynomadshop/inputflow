namespace InputFlow.Shared;

/// <summary>Display units for cursor distance.</summary>
public enum CursorDisplayUnit
{
    Centimeters,
    Meters,
    Kilometers,
    /// <summary>Auto: cm below 100cm, m below 1000m, km at/above 1000m.</summary>
    Auto
}
