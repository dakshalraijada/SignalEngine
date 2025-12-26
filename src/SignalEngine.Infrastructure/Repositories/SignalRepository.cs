using Microsoft.EntityFrameworkCore;
using SignalEngine.Application.Common.Interfaces;
using SignalEngine.Domain.Constants;
using SignalEngine.Domain.Entities;
using SignalEngine.Infrastructure.Persistence;

namespace SignalEngine.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Signal operations.
/// </summary>
public class SignalRepository : ISignalRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILookupRepository _lookupRepository;

    public SignalRepository(ApplicationDbContext context, ILookupRepository lookupRepository)
    {
        _context = context;
        _lookupRepository = lookupRepository;
    }

    public async Task<Signal?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Signals.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<Signal>> GetByTenantIdAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Signals
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.TriggeredAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Signal>> GetOpenByTenantIdAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        var openStatusId = await _lookupRepository.ResolveLookupIdAsync(
            LookupTypeCodes.SignalStatus, SignalStatusCodes.Open, cancellationToken);

        return await _context.Signals
            .Where(x => x.TenantId == tenantId && x.SignalStatusId == openStatusId)
            .OrderByDescending(x => x.TriggeredAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Signal>> GetByRuleIdAsync(int ruleId, CancellationToken cancellationToken = default)
    {
        return await _context.Signals
            .Where(x => x.RuleId == ruleId)
            .OrderByDescending(x => x.TriggeredAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Signal> AddAsync(Signal signal, CancellationToken cancellationToken = default)
    {
        await _context.Signals.AddAsync(signal, cancellationToken);
        return signal;
    }

    public Task UpdateAsync(Signal signal, CancellationToken cancellationToken = default)
    {
        _context.Signals.Update(signal);
        return Task.CompletedTask;
    }
}
