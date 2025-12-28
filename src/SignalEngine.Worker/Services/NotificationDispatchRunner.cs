using SignalEngine.Application.Common.Interfaces;

namespace SignalEngine.Worker.Services;

/// <summary>
/// Result of a notification dispatch cycle.
/// </summary>
public record NotificationDispatchResult(
    int NotificationsSent,
    int NotificationsFailed,
    int NotificationsSkipped);

/// <summary>
/// Scoped service that runs a single notification dispatch cycle.
/// Picks up pending notifications from the database and dispatches them via the configured channels.
/// 
/// Design principles:
/// - Single responsibility: Only handles notification dispatch, not signal creation
/// - Idempotent: Safe to run multiple times (notifications are marked sent/failed)
/// - Tenant-aware: Processes notifications for all tenants (system-level worker)
/// - Retry support: Notifications are retried up to MaxRetryCount times
/// </summary>
public class NotificationDispatchRunner
{
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<NotificationDispatchRunner> _logger;

    public NotificationDispatchRunner(
        INotificationRepository notificationRepository,
        INotificationDispatcher notificationDispatcher,
        IUnitOfWork unitOfWork,
        ILogger<NotificationDispatchRunner> logger)
    {
        _notificationRepository = notificationRepository;
        _notificationDispatcher = notificationDispatcher;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Runs a single notification dispatch cycle.
    /// </summary>
    /// <param name="maxNotifications">Maximum notifications to process.</param>
    /// <param name="maxRetryCount">Maximum retry attempts for failed notifications.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing counts of sent, failed, and skipped notifications.</returns>
    public async Task<NotificationDispatchResult> RunAsync(
        int maxNotifications,
        int maxRetryCount,
        CancellationToken cancellationToken)
    {
        // Get pending notifications (not sent, retry count < max)
        var pendingNotifications = await _notificationRepository.GetPendingAsync(cancellationToken);

        if (pendingNotifications.Count == 0)
        {
            _logger.LogDebug("No pending notifications to dispatch");
            return new NotificationDispatchResult(0, 0, 0);
        }

        _logger.LogInformation(
            "Found {Count} pending notifications to dispatch",
            pendingNotifications.Count);

        var sent = 0;
        var failed = 0;
        var skipped = 0;

        // Process up to maxNotifications
        var toProcess = pendingNotifications.Take(maxNotifications);

        foreach (var notification in toProcess)
        {
            // Skip if retry count exceeds max
            if (notification.RetryCount >= maxRetryCount)
            {
                _logger.LogWarning(
                    "Notification {NotificationId} exceeded max retry count ({RetryCount}/{MaxRetryCount}), skipping",
                    notification.Id,
                    notification.RetryCount,
                    maxRetryCount);
                skipped++;
                continue;
            }

            try
            {
                var success = await _notificationDispatcher.DispatchAsync(notification, cancellationToken);

                if (success)
                {
                    notification.MarkAsSent();
                    await _notificationRepository.UpdateAsync(notification, cancellationToken);
                    sent++;

                    _logger.LogDebug(
                        "Notification {NotificationId} sent successfully via channel {ChannelTypeId}",
                        notification.Id,
                        notification.ChannelTypeId);
                }
                else
                {
                    notification.MarkAsFailed("Dispatch returned false");
                    await _notificationRepository.UpdateAsync(notification, cancellationToken);
                    failed++;

                    _logger.LogWarning(
                        "Notification {NotificationId} dispatch failed (attempt {RetryCount})",
                        notification.Id,
                        notification.RetryCount);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                notification.MarkAsFailed(ex.Message);
                await _notificationRepository.UpdateAsync(notification, cancellationToken);
                failed++;

                _logger.LogError(
                    ex,
                    "Error dispatching notification {NotificationId}",
                    notification.Id);
            }
        }

        // Save all changes in a single transaction
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Notification dispatch complete: {Sent} sent, {Failed} failed, {Skipped} skipped",
            sent,
            failed,
            skipped);

        return new NotificationDispatchResult(sent, failed, skipped);
    }
}
