using MediatR;

namespace SignalEngine.Application.Metrics.Commands;

/// <summary>
/// Command to ingest metrics for all due assets.
/// This is called by the MetricIngestionWorker on each tick.
/// </summary>
public record IngestMetricsCommand : IRequest<IngestMetricsResult>;

/// <summary>
/// Result of metric ingestion cycle.
/// </summary>
public record IngestMetricsResult(
    /// <summary>
    /// Number of assets processed in this cycle.
    /// </summary>
    int AssetsProcessed,

    /// <summary>
    /// Number of data points inserted.
    /// </summary>
    int DataPointsCreated,

    /// <summary>
    /// Number of assets that failed to ingest.
    /// </summary>
    int Errors,

    /// <summary>
    /// Duration of the ingestion cycle.
    /// </summary>
    TimeSpan Duration)
{
    public static IngestMetricsResult Empty(TimeSpan duration) => new(0, 0, 0, duration);
}
