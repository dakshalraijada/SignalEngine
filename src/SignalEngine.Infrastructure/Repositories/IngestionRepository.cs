using Microsoft.EntityFrameworkCore;
using SignalEngine.Application.Common.Interfaces;
using SignalEngine.Domain.Entities;
using SignalEngine.Infrastructure.Persistence;

namespace SignalEngine.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for metric ingestion operations.
/// Optimized for the ingestion worker's access patterns.
/// </summary>
public class IngestionRepository : IIngestionRepository
{
    private readonly ApplicationDbContext _context;

    public IngestionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Asset>> GetAssetsDueForIngestionAsync(
        DateTime asOfUtc,
        CancellationToken cancellationToken = default)
    {
        // Query: Active assets where NextIngestionAtUtc is null (never ingested)
        // or NextIngestionAtUtc <= asOfUtc (due for ingestion)
        // Note: This bypasses tenant filtering because we're the system worker
        return await _context.Assets
            .Include(a => a.DataSource)
            .Include(a => a.Metrics.Where(m => m.IsActive))
            .Where(a => a.IsActive)
            .Where(a => a.NextIngestionAtUtc == null || a.NextIngestionAtUtc <= asOfUtc)
            .OrderBy(a => a.DataSourceId)
            .ThenBy(a => a.Identifier)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Metric>> GetActiveMetricsByAssetIdAsync(
        int assetId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Metrics
            .Where(m => m.AssetId == assetId && m.IsActive)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddMetricDataBatchAsync(
        IEnumerable<MetricData> dataPoints,
        CancellationToken cancellationToken = default)
    {
        await _context.MetricData.AddRangeAsync(dataPoints, cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateIngestionCursorAsync(
        int assetId,
        DateTime lastIngestedAtUtc,
        DateTime nextIngestionAtUtc,
        CancellationToken cancellationToken = default)
    {
        // Use ExecuteUpdateAsync for efficient single-row update without loading entity
        await _context.Assets
            .Where(a => a.Id == assetId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(a => a.LastIngestedAtUtc, lastIngestedAtUtc)
                .SetProperty(a => a.NextIngestionAtUtc, nextIngestionAtUtc),
                cancellationToken);
    }
}
