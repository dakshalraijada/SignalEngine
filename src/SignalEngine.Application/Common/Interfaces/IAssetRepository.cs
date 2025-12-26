using SignalEngine.Domain.Entities;

namespace SignalEngine.Application.Common.Interfaces;

/// <summary>
/// Repository interface for Asset operations.
/// </summary>
public interface IAssetRepository
{
    Task<Asset?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Asset>> GetByTenantIdAsync(int tenantId, CancellationToken cancellationToken = default);
    Task<int> GetCountByTenantIdAsync(int tenantId, CancellationToken cancellationToken = default);
    Task<Asset> AddAsync(Asset asset, CancellationToken cancellationToken = default);
    Task UpdateAsync(Asset asset, CancellationToken cancellationToken = default);
    Task DeleteAsync(Asset asset, CancellationToken cancellationToken = default);
}
