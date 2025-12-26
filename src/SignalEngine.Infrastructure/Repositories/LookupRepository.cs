using Microsoft.EntityFrameworkCore;
using SignalEngine.Application.Common.Interfaces;
using SignalEngine.Domain.Entities;
using SignalEngine.Domain.Exceptions;
using SignalEngine.Infrastructure.Persistence;

namespace SignalEngine.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for lookup operations.
/// </summary>
public class LookupRepository : ILookupRepository
{
    private readonly ApplicationDbContext _context;

    public LookupRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<LookupType>> GetAllLookupTypesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.LookupTypes
            .AsNoTracking()
            .OrderBy(x => x.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task<LookupType?> GetLookupTypeByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.LookupTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == code.ToUpperInvariant(), cancellationToken);
    }

    public async Task<LookupValue?> GetLookupValueByCodeAsync(string typeCode, string valueCode, CancellationToken cancellationToken = default)
    {
        return await _context.LookupValues
            .AsNoTracking()
            .Where(v => v.Code == valueCode.ToUpperInvariant())
            .Join(_context.LookupTypes.Where(t => t.Code == typeCode.ToUpperInvariant()),
                  v => v.LookupTypeId,
                  t => t.Id,
                  (v, t) => v)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<LookupValue?> GetLookupValueByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.LookupValues
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<LookupValue>> GetLookupValuesByTypeCodeAsync(string typeCode, CancellationToken cancellationToken = default)
    {
        var lookupType = await GetLookupTypeByCodeAsync(typeCode, cancellationToken);
        if (lookupType == null)
            return Array.Empty<LookupValue>();

        return await _context.LookupValues
            .AsNoTracking()
            .Where(x => x.LookupTypeId == lookupType.Id && x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> ResolveLookupIdAsync(string typeCode, string valueCode, CancellationToken cancellationToken = default)
    {
        var lookup = await GetLookupValueByCodeAsync(typeCode, valueCode, cancellationToken);
        if (lookup == null)
            throw new LookupNotFoundException(typeCode, valueCode);

        return lookup.Id;
    }

    public async Task<string> ResolveLookupCodeAsync(int lookupValueId, CancellationToken cancellationToken = default)
    {
        var lookup = await GetLookupValueByIdAsync(lookupValueId, cancellationToken);
        if (lookup == null)
            throw new EntityNotFoundException("LookupValue", lookupValueId);

        return lookup.Code;
    }
}
