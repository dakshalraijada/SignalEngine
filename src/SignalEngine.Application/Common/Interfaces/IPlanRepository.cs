using SignalEngine.Domain.Entities;

namespace SignalEngine.Application.Common.Interfaces;

/// <summary>
/// Repository interface for Plan operations.
/// </summary>
public interface IPlanRepository
{
    Task<Plan?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Plan?> GetByPlanCodeIdAsync(int planCodeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Plan>> GetAllActiveAsync(CancellationToken cancellationToken = default);
}
