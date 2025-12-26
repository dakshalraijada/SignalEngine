using SignalEngine.Domain.Entities;

namespace SignalEngine.Application.Common.Interfaces;

/// <summary>
/// Repository interface for SignalState operations.
/// </summary>
public interface ISignalStateRepository
{
    Task<SignalState?> GetByRuleIdAsync(int ruleId, CancellationToken cancellationToken = default);
    Task<SignalState> AddAsync(SignalState signalState, CancellationToken cancellationToken = default);
    Task UpdateAsync(SignalState signalState, CancellationToken cancellationToken = default);
    Task<SignalState> GetOrCreateAsync(int tenantId, int ruleId, CancellationToken cancellationToken = default);
}
