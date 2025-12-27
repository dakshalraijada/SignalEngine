using Microsoft.EntityFrameworkCore;
using SignalEngine.Application.Common.Interfaces;
using SignalEngine.Domain.Entities;
using SignalEngine.Infrastructure.Persistence;

namespace SignalEngine.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Metric (definition) operations.
/// </summary>
public class MetricRepository : IMetricRepository
{
    private readonly ApplicationDbContext _context;

    public MetricRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Metric?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Metrics.FindAsync([id], cancellationToken);
    }

    public async Task<Metric?> GetByAssetAndNameAsync(int assetId, string metricName, CancellationToken cancellationToken = default)
    {
        return await _context.Metrics
            .Where(x => x.AssetId == assetId && x.Name == metricName)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Metric>> GetByAssetIdAsync(int assetId, CancellationToken cancellationToken = default)
    {
        return await _context.Metrics
            .Where(x => x.AssetId == assetId)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Metric> AddAsync(Metric metric, CancellationToken cancellationToken = default)
    {
        await _context.Metrics.AddAsync(metric, cancellationToken);
        return metric;
    }

    public Task UpdateAsync(Metric metric, CancellationToken cancellationToken = default)
    {
        _context.Metrics.Update(metric);
        return Task.CompletedTask;
    }
}
