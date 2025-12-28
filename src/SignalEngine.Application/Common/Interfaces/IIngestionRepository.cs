using SignalEngine.Domain.Entities;

namespace SignalEngine.Application.Common.Interfaces;

/// <summary>
/// Repository interface for metric ingestion operations.
/// Provides efficient queries for the ingestion worker.
/// </summary>
public interface IIngestionRepository
{
    /// <summary>
    /// Gets all active assets that are due for ingestion.
    /// An asset is due if: IsActive = true AND (NextIngestionAtUtc IS NULL OR NextIngestionAtUtc <= asOfUtc)
    /// </summary>
    /// <param name="asOfUtc">The reference time (typically DateTime.UtcNow).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Assets due for ingestion, including DataSource navigation property.</returns>
    Task<IReadOnlyList<Asset>> GetAssetsDueForIngestionAsync(
        DateTime asOfUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets metrics for a specific asset.
    /// Used to determine which metrics to create data points for after fetching from data source.
    /// </summary>
    /// <param name="assetId">The asset ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Active metrics for the asset.</returns>
    Task<IReadOnlyList<Metric>> GetActiveMetricsByAssetIdAsync(
        int assetId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk adds metric data points.
    /// </summary>
    /// <param name="dataPoints">The data points to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddMetricDataBatchAsync(
        IEnumerable<MetricData> dataPoints,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the ingestion cursor for an asset.
    /// </summary>
    /// <param name="assetId">The asset ID.</param>
    /// <param name="lastIngestedAtUtc">When ingestion completed.</param>
    /// <param name="nextIngestionAtUtc">When next ingestion is due.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateIngestionCursorAsync(
        int assetId,
        DateTime lastIngestedAtUtc,
        DateTime nextIngestionAtUtc,
        CancellationToken cancellationToken = default);
}
