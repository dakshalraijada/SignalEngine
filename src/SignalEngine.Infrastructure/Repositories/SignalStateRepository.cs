using Microsoft.EntityFrameworkCore;
using SignalEngine.Application.Common.Interfaces;
using SignalEngine.Domain.Entities;
using SignalEngine.Infrastructure.Persistence;

namespace SignalEngine.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for SignalState operations.
/// </summary>
public class SignalStateRepository : ISignalStateRepository
{
    private readonly ApplicationDbContext _context;

    public SignalStateRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SignalState?> GetByRuleIdAsync(int ruleId, CancellationToken cancellationToken = default)
    {
        return await _context.SignalStates
            .FirstOrDefaultAsync(x => x.RuleId == ruleId, cancellationToken);
    }

    public async Task<SignalState> AddAsync(SignalState signalState, CancellationToken cancellationToken = default)
    {
        await _context.SignalStates.AddAsync(signalState, cancellationToken);
        return signalState;
    }

    public Task UpdateAsync(SignalState signalState, CancellationToken cancellationToken = default)
    {
        _context.SignalStates.Update(signalState);
        return Task.CompletedTask;
    }

    public async Task<SignalState> GetOrCreateAsync(int tenantId, int ruleId, CancellationToken cancellationToken = default)
    {
        var state = await GetByRuleIdAsync(ruleId, cancellationToken);
        if (state == null)
        {
            state = new SignalState(tenantId, ruleId);
            await AddAsync(state, cancellationToken);
        }
        return state;
    }
}
