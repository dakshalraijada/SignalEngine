using SignalEngine.Domain.Entities;

namespace SignalEngine.Application.Common.Interfaces;

/// <summary>
/// Repository interface for lookup operations.
/// </summary>
public interface ILookupRepository
{
    Task<IReadOnlyList<LookupType>> GetAllLookupTypesAsync(CancellationToken cancellationToken = default);
    Task<LookupType?> GetLookupTypeByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<LookupValue?> GetLookupValueByCodeAsync(string typeCode, string valueCode, CancellationToken cancellationToken = default);
    Task<LookupValue?> GetLookupValueByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LookupValue>> GetLookupValuesByTypeCodeAsync(string typeCode, CancellationToken cancellationToken = default);
    Task<int> ResolveLookupIdAsync(string typeCode, string valueCode, CancellationToken cancellationToken = default);
    Task<string> ResolveLookupCodeAsync(int lookupValueId, CancellationToken cancellationToken = default);
}
