using SignalEngine.Domain.Entities;

namespace SignalEngine.Application.Common.Interfaces;

/// <summary>
/// Repository interface for SignalResolution operations.
/// </summary>
public interface ISignalResolutionRepository
{
    Task<SignalResolution?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<SignalResolution?> GetLatestBySignalIdAsync(int signalId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SignalResolution>> GetBySignalIdAsync(int signalId, CancellationToken cancellationToken = default);
    Task<SignalResolution> AddAsync(SignalResolution resolution, CancellationToken cancellationToken = default);
}
