using System;

namespace InputFlow.Shared;

/// <summary>Aggregate keyboard metrics for a reporting period.</summary>
public record KeyMetrics(
    int TotalKeyPresses,
    int KeyPressesLast60Seconds,
    bool IsLastWindowPartial,  // true if the full 60s was not observed
    DateTime Timestamp);
