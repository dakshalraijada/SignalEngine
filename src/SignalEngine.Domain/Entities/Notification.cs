using SignalEngine.Domain.Common;

namespace SignalEngine.Domain.Entities;

/// <summary>
/// Represents a notification sent when a signal is triggered.
/// ChannelTypeId references LookupValues (NOTIFICATION_CHANNEL_TYPE: EMAIL, WEBHOOK, SLACK).
/// </summary>
public class Notification : AuditableEntity, ITenantScoped
{
    public int TenantId { get; private set; }
    public int SignalId { get; private set; }
    public int ChannelTypeId { get; private set; }
    public string Recipient { get; private set; } = null!;
    public string Subject { get; private set; } = null!;
    public string Body { get; private set; } = null!;
    public DateTime? SentAt { get; private set; }
    public bool IsSent { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int RetryCount { get; private set; }

    // Navigation properties
    public LookupValue? ChannelType { get; private set; }
    public Signal? Signal { get; private set; }

    private Notification() { } // EF Core

    public Notification(
        int tenantId,
        int signalId,
        int channelTypeId,
        string recipient,
        string subject,
        string body)
    {
        if (tenantId <= 0)
            throw new ArgumentException("Tenant ID must be positive.", nameof(tenantId));

        if (signalId <= 0)
            throw new ArgumentException("Signal ID must be positive.", nameof(signalId));

        if (channelTypeId <= 0)
            throw new ArgumentException("Channel type ID must be positive.", nameof(channelTypeId));

        if (string.IsNullOrWhiteSpace(recipient))
            throw new ArgumentException("Recipient is required.", nameof(recipient));

        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Subject is required.", nameof(subject));

        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("Body is required.", nameof(body));

        TenantId = tenantId;
        SignalId = signalId;
        ChannelTypeId = channelTypeId;
        Recipient = recipient;
        Subject = subject;
        Body = body;
        IsSent = false;
        RetryCount = 0;
    }

    public void MarkAsSent()
    {
        IsSent = true;
        SentAt = DateTime.UtcNow;
        ErrorMessage = null;
    }

    public void MarkAsFailed(string errorMessage)
    {
        IsSent = false;
        ErrorMessage = errorMessage;
        RetryCount++;
    }
}
