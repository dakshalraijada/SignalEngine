using SignalEngine.Domain.Entities;

namespace SignalEngine.Application.Common.Interfaces;

/// <summary>
/// Repository interface for MetricData (time-series) operations.
/// </summary>
public interface IMetricDataRepository
{
    Task<MetricData?> GetLatestByMetricIdAsync(int metricId, CancellationToken cancellationToken = default);
    Task<MetricData?> GetLatestByAssetAndNameAsync(int assetId, string metricName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MetricData>> GetByMetricIdAsync(int metricId, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
    Task<MetricData> AddAsync(MetricData metricData, CancellationToken cancellationToken = default);
}
