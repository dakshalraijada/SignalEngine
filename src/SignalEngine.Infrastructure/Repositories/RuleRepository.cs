using Microsoft.EntityFrameworkCore;
using SignalEngine.Application.Common.Interfaces;
using SignalEngine.Domain.Entities;
using SignalEngine.Infrastructure.Persistence;

namespace SignalEngine.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Rule operations.
/// </summary>
public class RuleRepository : IRuleRepository
{
    private readonly ApplicationDbContext _context;

    public RuleRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Rule?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Rules.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<Rule>> GetByTenantIdAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Rules
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Rule>> GetActiveByTenantIdAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Rules
            .Where(x => x.TenantId == tenantId && x.IsActive)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Rule>> GetByEvaluationFrequencyIdAsync(int evaluationFrequencyId, CancellationToken cancellationToken = default)
    {
        return await _context.Rules
            .Where(x => x.EvaluationFrequencyId == evaluationFrequencyId && x.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Rule>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Rules
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCountByTenantIdAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Rules
            .CountAsync(x => x.TenantId == tenantId, cancellationToken);
    }

    public async Task<Rule> AddAsync(Rule rule, CancellationToken cancellationToken = default)
    {
        await _context.Rules.AddAsync(rule, cancellationToken);
        return rule;
    }

    public Task UpdateAsync(Rule rule, CancellationToken cancellationToken = default)
    {
        _context.Rules.Update(rule);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Rule rule, CancellationToken cancellationToken = default)
    {
        _context.Rules.Remove(rule);
        return Task.CompletedTask;
    }
}
