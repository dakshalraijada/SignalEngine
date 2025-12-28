using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SignalEngine.Application.Common.Interfaces;
using SignalEngine.Domain.Constants;
using SignalEngine.Domain.Entities;
using SignalEngine.Infrastructure.Services.Email;

namespace SignalEngine.Infrastructure.Services;

/// <summary>
/// Notification dispatcher implementation.
/// Dispatches notifications to various channels (Email, Webhook, Slack).
/// 
/// Currently implemented:
/// - Webhook: Real HTTP POST to the recipient URL
/// - Email: Real SMTP delivery via IEmailSender
/// - Slack: Stub (logs only)
/// </summary>
public class NotificationDispatcher : INotificationDispatcher
{
    private readonly ILookupRepository _lookupRepository;
    private readonly HttpClient _httpClient;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<NotificationDispatcher> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public NotificationDispatcher(
        ILookupRepository lookupRepository,
        HttpClient httpClient,
        IEmailSender emailSender,
        ILogger<NotificationDispatcher> logger)
    {
        _lookupRepository = lookupRepository;
        _httpClient = httpClient;
        _emailSender = emailSender;
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

    private async Task<bool> SendEmailAsync(Notification notification, CancellationToken cancellationToken)
    {
        var recipient = notification.Recipient;

        // Validate recipient email address
        if (string.IsNullOrWhiteSpace(recipient))
        {
            _logger.LogWarning(
                "Email notification {NotificationId} has no recipient address",
                notification.Id);
            return false;
        }

        // Basic email format validation
        if (!recipient.Contains('@') || !recipient.Contains('.'))
        {
            _logger.LogWarning(
                "Email notification {NotificationId} has invalid recipient address: {Recipient}",
                notification.Id,
                recipient);
            return false;
        }

        try
        {
            await _emailSender.SendAsync(
                recipient,
                notification.Subject,
                notification.Body,
                cancellationToken);

            _logger.LogInformation(
                "Email notification {NotificationId} sent successfully to {Recipient}",
                notification.Id,
                recipient);

            return true;
        }
        catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send email notification {NotificationId} to {Recipient}",
                notification.Id,
                recipient);
            return false;
        }
    }

    /// <summary>
    /// Sends a notification via HTTP POST to the webhook URL.
    /// The Recipient field is expected to contain the webhook URL.
    /// </summary>
    private async Task<bool> SendWebhookAsync(Notification notification, CancellationToken cancellationToken)
    {
        var webhookUrl = notification.Recipient;

        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            _logger.LogWarning(
                "Webhook notification {NotificationId} has no recipient URL",
                notification.Id);
            return false;
        }

        // Validate URL format
        if (!Uri.TryCreate(webhookUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            _logger.LogWarning(
                "Webhook notification {NotificationId} has invalid URL: {Url}",
                notification.Id,
                webhookUrl);
            return false;
        }

        try
        {
            // Build webhook payload
            var payload = new WebhookPayload
            {
                NotificationId = notification.Id,
                SignalId = notification.SignalId,
                Subject = notification.Subject,
                Body = notification.Body,
                CreatedAt = notification.CreatedAt,
                Timestamp = DateTime.UtcNow
            };

            _logger.LogDebug(
                "Sending webhook notification {NotificationId} to {Url}",
                notification.Id,
                webhookUrl);

            var response = await _httpClient.PostAsJsonAsync(
                webhookUrl,
                payload,
                JsonOptions,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Webhook notification {NotificationId} sent successfully to {Url}. Status: {StatusCode}",
                    notification.Id,
                    webhookUrl,
                    response.StatusCode);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Webhook notification {NotificationId} failed. Status: {StatusCode}, Response: {Response}",
                    notification.Id,
                    response.StatusCode,
                    errorContent.Length > 500 ? errorContent[..500] : errorContent);
                return false;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "HTTP error sending webhook notification {NotificationId} to {Url}",
                notification.Id,
                webhookUrl);
            return false;
        }
        catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error sending webhook notification {NotificationId} to {Url}",
                notification.Id,
                webhookUrl);
            return false;
        }
    }

    private Task<bool> SendSlackAsync(Notification notification, CancellationToken cancellationToken)
    {
        // Stub implementation - replace with actual Slack service
        // Would typically POST to Slack incoming webhook URL
        _logger.LogInformation(
            "Slack notification sent to {Recipient}: {Subject}",
            notification.Recipient,
            notification.Subject);

        return Task.FromResult(true);
    }

    /// <summary>
    /// Payload sent to webhook endpoints.
    /// </summary>
    private sealed class WebhookPayload
    {
        public int NotificationId { get; set; }
        public int SignalId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
