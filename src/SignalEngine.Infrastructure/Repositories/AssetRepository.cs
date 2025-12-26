using Microsoft.EntityFrameworkCore;
using SignalEngine.Application.Common.Interfaces;
using SignalEngine.Domain.Entities;
using SignalEngine.Infrastructure.Persistence;

namespace SignalEngine.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Asset operations.
/// </summary>
public class AssetRepository : IAssetRepository
{
    private readonly ApplicationDbContext _context;

    public AssetRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Asset?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Assets.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<Asset>> GetByTenantIdAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Assets
            .Where(x => x.TenantId == tenantId && x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCountByTenantIdAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Assets
            .CountAsync(x => x.TenantId == tenantId, cancellationToken);
    }

    public async Task<Asset> AddAsync(Asset asset, CancellationToken cancellationToken = default)
    {
        await _context.Assets.AddAsync(asset, cancellationToken);
        return asset;
    }

    public Task UpdateAsync(Asset asset, CancellationToken cancellationToken = default)
    {
        _context.Assets.Update(asset);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Asset asset, CancellationToken cancellationToken = default)
    {
        _context.Assets.Remove(asset);
        return Task.CompletedTask;
    }
}
