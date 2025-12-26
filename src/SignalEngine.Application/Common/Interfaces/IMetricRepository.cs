using SignalEngine.Domain.Entities;

namespace SignalEngine.Application.Common.Interfaces;

/// <summary>
/// Repository interface for Metric operations.
/// </summary>
public interface IMetricRepository
{
    Task<Metric?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Metric>> GetByAssetIdAsync(int assetId, CancellationToken cancellationToken = default);
    Task<Metric?> GetLatestByAssetAndNameAsync(int assetId, string metricName, CancellationToken cancellationToken = default);
    Task<Metric> AddAsync(Metric metric, CancellationToken cancellationToken = default);
    Task UpdateAsync(Metric metric, CancellationToken cancellationToken = default);
}
