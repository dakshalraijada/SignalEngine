using Microsoft.EntityFrameworkCore;
using SignalEngine.Application.Common.Interfaces;
using SignalEngine.Domain.Entities;
using SignalEngine.Infrastructure.Persistence;

namespace SignalEngine.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for MetricData (time-series) operations.
/// </summary>
public class MetricDataRepository : IMetricDataRepository
{
    private readonly ApplicationDbContext _context;

    public MetricDataRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<MetricData?> GetLatestByMetricIdAsync(int metricId, CancellationToken cancellationToken = default)
    {
        return await _context.MetricData
            .Where(x => x.MetricId == metricId)
            .OrderByDescending(x => x.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<MetricData?> GetLatestByAssetAndNameAsync(int assetId, string metricName, CancellationToken cancellationToken = default)
    {
        return await _context.MetricData
            .Include(x => x.Metric)
            .Where(x => x.Metric.AssetId == assetId && x.Metric.Name == metricName)
            .OrderByDescending(x => x.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MetricData>> GetByMetricIdAsync(
        int metricId, 
        DateTime? from = null, 
        DateTime? to = null, 
        CancellationToken cancellationToken = default)
    {
        var query = _context.MetricData.Where(x => x.MetricId == metricId);

        if (from.HasValue)
            query = query.Where(x => x.Timestamp >= from.Value);

        if (to.HasValue)
            query = query.Where(x => x.Timestamp <= to.Value);

        return await query
            .OrderBy(x => x.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<MetricData> AddAsync(MetricData metricData, CancellationToken cancellationToken = default)
    {
        await _context.MetricData.AddAsync(metricData, cancellationToken);
        return metricData;
    }
}
