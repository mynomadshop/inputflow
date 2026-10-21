using System;

namespace InputFlow.Shared;

/// <summary>User-configurable application settings.</summary>
public record AppSettings(
    CursorDisplayUnit DistanceUnit,
    bool KeyboardTrackingEnabled,
    bool PointerTrackingEnabled,
    string Theme,  // "dark", "light", or "system"
    bool AnimationsEnabled,
    bool StartWithWindows,
    CloseBehavior CloseBehavior,
    int RetentionDays30SecBuckets,
    int RetentionMonthsDailySummaries,
    PrivacyConsentLevel ConsentLevel,
    DateTimeOffset? LastConsentUpdate);

/// <summary>What happens when the user clicks the window close button.</summary>
public enum CloseBehavior { MinimizeToTray, QuitImmediately }

/// <summary>User's consent level for data collection.</summary>
public enum PrivacyConsentLevel { NotGiven, Given }
