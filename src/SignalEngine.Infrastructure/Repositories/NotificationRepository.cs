using Microsoft.EntityFrameworkCore;
using SignalEngine.Application.Common.Interfaces;
using SignalEngine.Domain.Entities;
using SignalEngine.Infrastructure.Persistence;

namespace SignalEngine.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Notification operations.
/// </summary>
public class NotificationRepository : INotificationRepository
{
    private readonly ApplicationDbContext _context;

    public NotificationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Notification?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<Notification>> GetBySignalIdAsync(int signalId, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .Where(x => x.SignalId == signalId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Notification>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .Where(x => !x.IsSent && x.RetryCount < 3)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Notification> AddAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        await _context.Notifications.AddAsync(notification, cancellationToken);
        return notification;
    }

    public Task UpdateAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        _context.Notifications.Update(notification);
        return Task.CompletedTask;
    }
}
