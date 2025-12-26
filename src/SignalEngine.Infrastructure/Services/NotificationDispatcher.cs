using Microsoft.Extensions.Logging;
using SignalEngine.Application.Common.Interfaces;
using SignalEngine.Domain.Constants;
using SignalEngine.Domain.Entities;

namespace SignalEngine.Infrastructure.Services;

/// <summary>
/// Notification dispatcher implementation.
/// </summary>
public class NotificationDispatcher : INotificationDispatcher
{
    private readonly ILookupRepository _lookupRepository;
    private readonly ILogger<NotificationDispatcher> _logger;

    public NotificationDispatcher(
        ILookupRepository lookupRepository,
        ILogger<NotificationDispatcher> logger)
    {
        _lookupRepository = lookupRepository;
        _logger = logger;
    }

    public async Task<bool> DispatchAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        try
        {
            var channelCode = await _lookupRepository.ResolveLookupCodeAsync(notification.ChannelTypeId, cancellationToken);

            return channelCode switch
            {
                NotificationChannelTypeCodes.Email => await SendEmailAsync(notification, cancellationToken),
                NotificationChannelTypeCodes.Webhook => await SendWebhookAsync(notification, cancellationToken),
                NotificationChannelTypeCodes.Slack => await SendSlackAsync(notification, cancellationToken),
                _ => false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch notification {NotificationId}", notification.Id);
            return false;
        }
    }

    private Task<bool> SendEmailAsync(Notification notification, CancellationToken cancellationToken)
    {
        // Stub implementation - replace with actual email service
        _logger.LogInformation(
            "Email notification sent to {Recipient}: {Subject}",
            notification.Recipient,
            notification.Subject);

        return Task.FromResult(true);
    }

    private Task<bool> SendWebhookAsync(Notification notification, CancellationToken cancellationToken)
    {
        // Stub implementation - replace with actual webhook service
        _logger.LogInformation(
            "Webhook notification sent to {Recipient}: {Subject}",
            notification.Recipient,
            notification.Subject);

        return Task.FromResult(true);
    }

    private Task<bool> SendSlackAsync(Notification notification, CancellationToken cancellationToken)
    {
        // Stub implementation - replace with actual Slack service
        _logger.LogInformation(
            "Slack notification sent to {Recipient}: {Subject}",
            notification.Recipient,
            notification.Subject);

        return Task.FromResult(true);
    }
}
