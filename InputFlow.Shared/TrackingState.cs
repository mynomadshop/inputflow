namespace InputFlow.Shared;

/// <summary>The current state of the tracking session.</summary>
public enum TrackingState
{
    /// <summary>Not tracking — initial state or after explicit stop.</summary>
    Stopped,

    /// <summary>Actively collecting keyboard and pointer events.</summary>
    Collecting,

    /// <summary>Paused by the user; counters frozen until resumed.</summary>
    Paused
}
