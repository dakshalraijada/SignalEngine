using Microsoft.EntityFrameworkCore;
using SignalEngine.Application.Common.Interfaces;
using SignalEngine.Domain.Entities;
using SignalEngine.Infrastructure.Persistence;

namespace SignalEngine.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Plan operations.
/// </summary>
public class PlanRepository : IPlanRepository
{
    private readonly ApplicationDbContext _context;

    public PlanRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Plan?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Plans.FindAsync([id], cancellationToken);
    }

    public async Task<Plan?> GetByPlanCodeIdAsync(int planCodeId, CancellationToken cancellationToken = default)
    {
        return await _context.Plans
            .FirstOrDefaultAsync(x => x.PlanCodeId == planCodeId && x.IsActive, cancellationToken);
    }

    public async Task<IReadOnlyList<Plan>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Plans
            .Where(x => x.IsActive)
            .OrderBy(x => x.MonthlyPrice)
            .ToListAsync(cancellationToken);
    }
}
