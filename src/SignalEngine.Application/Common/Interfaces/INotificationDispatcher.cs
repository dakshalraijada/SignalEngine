using SignalEngine.Domain.Entities;

namespace SignalEngine.Application.Common.Interfaces;

/// <summary>
/// Interface for notification dispatch service.
/// </summary>
public interface INotificationDispatcher
{
    Task<bool> DispatchAsync(Notification notification, CancellationToken cancellationToken = default);
}
