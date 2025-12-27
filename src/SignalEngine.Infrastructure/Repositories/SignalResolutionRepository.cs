using Microsoft.EntityFrameworkCore;
using SignalEngine.Application.Common.Interfaces;
using SignalEngine.Domain.Entities;
using SignalEngine.Infrastructure.Persistence;

namespace SignalEngine.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for SignalResolution operations.
/// </summary>
public class SignalResolutionRepository : ISignalResolutionRepository
{
    private readonly ApplicationDbContext _context;

    public SignalResolutionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SignalResolution?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.SignalResolutions.FindAsync([id], cancellationToken);
    }

    public async Task<SignalResolution?> GetLatestBySignalIdAsync(int signalId, CancellationToken cancellationToken = default)
    {
        return await _context.SignalResolutions
            .Where(x => x.SignalId == signalId)
            .OrderByDescending(x => x.ResolvedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SignalResolution>> GetBySignalIdAsync(int signalId, CancellationToken cancellationToken = default)
    {
        return await _context.SignalResolutions
            .Where(x => x.SignalId == signalId)
            .OrderByDescending(x => x.ResolvedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<SignalResolution> AddAsync(SignalResolution resolution, CancellationToken cancellationToken = default)
    {
        await _context.SignalResolutions.AddAsync(resolution, cancellationToken);
        return resolution;
    }
}
