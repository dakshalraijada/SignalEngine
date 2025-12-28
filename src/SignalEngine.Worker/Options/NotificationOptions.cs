namespace SignalEngine.Worker.Options;

/// <summary>
/// Configuration options for the notification dispatch worker.
/// </summary>
public class NotificationOptions
{
    /// <summary>
    /// The interval between notification dispatch cycles.
    /// Default: 30 seconds.
    /// </summary>
    public TimeSpan TickInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The tick interval in seconds. Used for JSON configuration.
    /// </summary>
    public int TickIntervalSeconds
    {
        get => (int)TickInterval.TotalSeconds;
        set => TickInterval = TimeSpan.FromSeconds(value);
    }

    /// <summary>
    /// Maximum number of notifications to process per tick.
    /// Default: 100.
    /// </summary>
    public int MaxNotificationsPerTick { get; set; } = 100;

    /// <summary>
    /// Maximum retry count for failed notifications.
    /// Default: 3.
    /// </summary>
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>
    /// Whether notification dispatch is enabled.
    /// Default: true.
    /// </summary>
    public bool Enabled { get; set; } = true;
}
