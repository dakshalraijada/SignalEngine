using SignalEngine.Domain.Entities;

namespace SignalEngine.Application.Common.Interfaces;

/// <summary>
/// Repository interface for Signal operations.
/// </summary>
public interface ISignalRepository
{
    Task<Signal?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Signal>> GetByTenantIdAsync(int tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Signal>> GetOpenByTenantIdAsync(int tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Signal>> GetByRuleIdAsync(int ruleId, CancellationToken cancellationToken = default);
    Task<Signal> AddAsync(Signal signal, CancellationToken cancellationToken = default);
    Task UpdateAsync(Signal signal, CancellationToken cancellationToken = default);
}
