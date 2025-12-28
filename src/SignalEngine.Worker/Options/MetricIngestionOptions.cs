namespace SignalEngine.Worker.Options;

/// <summary>
/// Configuration options for metric ingestion.
/// </summary>
public class MetricIngestionOptions
{
    public const string SectionName = "MetricIngestion";

    /// <summary>
    /// Base tick interval in seconds.
    /// The worker wakes up at this interval to check for due assets.
    /// Default is 15 seconds. Recommended range: 10-30 seconds.
    /// </summary>
    public int TickIntervalSeconds { get; set; } = 15;

    /// <summary>
    /// Maximum number of assets to process per tick.
    /// Prevents overwhelming the system if many assets become due simultaneously.
    /// Default is 1000.
    /// </summary>
    public int MaxAssetsPerTick { get; set; } = 1000;

    /// <summary>
    /// Whether ingestion is enabled.
    /// Can be used to disable ingestion without stopping the worker.
    /// Default is true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets the tick interval as a TimeSpan.
    /// </summary>
    public TimeSpan TickInterval => TimeSpan.FromSeconds(TickIntervalSeconds);
}
