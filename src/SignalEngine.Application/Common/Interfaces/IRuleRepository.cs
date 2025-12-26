using SignalEngine.Domain.Entities;

namespace SignalEngine.Application.Common.Interfaces;

/// <summary>
/// Repository interface for Rule operations.
/// </summary>
public interface IRuleRepository
{
    Task<Rule?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Rule>> GetByTenantIdAsync(int tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Rule>> GetActiveByTenantIdAsync(int tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Rule>> GetByEvaluationFrequencyIdAsync(int evaluationFrequencyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Rule>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<int> GetCountByTenantIdAsync(int tenantId, CancellationToken cancellationToken = default);
    Task<Rule> AddAsync(Rule rule, CancellationToken cancellationToken = default);
    Task UpdateAsync(Rule rule, CancellationToken cancellationToken = default);
    Task DeleteAsync(Rule rule, CancellationToken cancellationToken = default);
}
