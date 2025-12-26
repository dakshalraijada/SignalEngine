using SignalEngine.Domain.Entities;

namespace SignalEngine.Application.Common.Interfaces;

/// <summary>
/// Repository interface for Notification operations.
/// </summary>
public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Notification>> GetBySignalIdAsync(int signalId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Notification>> GetPendingAsync(CancellationToken cancellationToken = default);
    Task<Notification> AddAsync(Notification notification, CancellationToken cancellationToken = default);
    Task UpdateAsync(Notification notification, CancellationToken cancellationToken = default);
}
