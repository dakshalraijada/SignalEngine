using Microsoft.EntityFrameworkCore;
using SignalEngine.Application.Common.Interfaces;
using SignalEngine.Domain.Entities;
using SignalEngine.Infrastructure.Persistence;

namespace SignalEngine.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Rule Evaluation Worker operations.
/// 
/// Key design decisions:
/// - All queries bypass tenant filtering (system-level operation)
/// - Eagerly loads navigation properties needed for evaluation
/// - Uses AsNoTracking where possible for read operations
/// - SignalState is tracked for change detection
/// </summary>
public class RuleEvaluationRepository : IRuleEvaluationRepository
{
    private readonly ApplicationDbContext _context;

    public RuleEvaluationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Rule>> GetActiveRulesWithDependenciesAsync(
        CancellationToken cancellationToken = default)
    {
        // Note: We explicitly bypass tenant filtering here because the worker
        // evaluates rules across all tenants. The ApplicationDbContext should
        // have null tenant context for system operations.
        return await _context.Rules
            .Include(r => r.Operator)
            .Include(r => r.Severity)
            .Include(r => r.Asset)
            .Where(r => r.IsActive)
            .AsNoTracking() // Rules are read-only in evaluation context
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<MetricData?> GetLatestMetricValueAsync(
        int assetId,
        string metricName,
        CancellationToken cancellationToken = default)
    {
        // Query through Metric to match by asset and name
        // Get the most recent data point by timestamp
        return await _context.MetricData
            .Include(md => md.Metric)
            .Where(md => md.Metric.AssetId == assetId)
            .Where(md => md.Metric.Name == metricName) // SQL Server default is case-insensitive
            .Where(md => md.Metric.IsActive)
            .OrderByDescending(md => md.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SignalState> GetOrCreateSignalStateAsync(
        int tenantId,
        int ruleId,
        CancellationToken cancellationToken = default)
    {
        // Try to find existing state - must be tracked for updates
        var state = await _context.SignalStates
            .FirstOrDefaultAsync(s => s.RuleId == ruleId, cancellationToken);

        if (state == null)
        {
            // Create new state - will be tracked automatically
            state = new SignalState(tenantId, ruleId);
            await _context.SignalStates.AddAsync(state, cancellationToken);
        }

        return state;
    }

    /// <inheritdoc />
    public async Task AddSignalAsync(
        Signal signal,
        CancellationToken cancellationToken = default)
    {
        await _context.Signals.AddAsync(signal, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> ResolveLookupCodeAsync(
        int lookupValueId,
        CancellationToken cancellationToken = default)
    {
        var lookupValue = await _context.LookupValues
            .AsNoTracking()
            .FirstOrDefaultAsync(lv => lv.Id == lookupValueId, cancellationToken);

        return lookupValue?.Code 
            ?? throw new InvalidOperationException($"Lookup value {lookupValueId} not found");
    }

    /// <inheritdoc />
    public async Task<int> ResolveLookupIdAsync(
        string typeCode,
        string valueCode,
        CancellationToken cancellationToken = default)
    {
        var lookupValue = await _context.LookupValues
            .Include(lv => lv.LookupType)
            .AsNoTracking()
            .FirstOrDefaultAsync(
                lv => lv.LookupType!.Code == typeCode && lv.Code == valueCode,
                cancellationToken);

        return lookupValue?.Id 
            ?? throw new InvalidOperationException($"Lookup value {typeCode}/{valueCode} not found");
    }
}
