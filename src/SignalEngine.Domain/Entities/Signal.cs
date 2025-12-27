using SignalEngine.Domain.Common;

namespace SignalEngine.Domain.Entities;

/// <summary>
/// Represents a signal generated when a rule is breached.
/// Signals are immutable after creation - resolutions are tracked via SignalResolution.
/// SignalStatusId references LookupValues (SIGNAL_STATUS: OPEN).
/// </summary>
public class Signal : AuditableEntity, ITenantScoped
{
    public int TenantId { get; private set; }
    public int RuleId { get; private set; }
    public int AssetId { get; private set; }
    public int SignalStatusId { get; private set; }
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public decimal TriggerValue { get; private set; }
    public decimal ThresholdValue { get; private set; }
    public DateTime TriggeredAt { get; private set; }

    // Navigation properties
    public LookupValue? SignalStatus { get; private set; }
    public Asset? Asset { get; private set; }
    public Rule? Rule { get; private set; }
    public SignalResolution? Resolution { get; private set; }

    private readonly List<Notification> _notifications = new();
    public IReadOnlyCollection<Notification> Notifications => _notifications.AsReadOnly();

    private Signal() { } // EF Core

    public Signal(
        int tenantId,
        int ruleId,
        int assetId,
        int signalStatusId,
        string title,
        decimal triggerValue,
        decimal thresholdValue,
        DateTime triggeredAt,
        string? description = null)
    {
        if (tenantId <= 0)
            throw new ArgumentException("Tenant ID must be positive.", nameof(tenantId));

        if (ruleId <= 0)
            throw new ArgumentException("Rule ID must be positive.", nameof(ruleId));

        if (assetId <= 0)
            throw new ArgumentException("Asset ID must be positive.", nameof(assetId));

        if (signalStatusId <= 0)
            throw new ArgumentException("Signal status ID must be positive.", nameof(signalStatusId));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Signal title is required.", nameof(title));

        TenantId = tenantId;
        RuleId = ruleId;
        AssetId = assetId;
        SignalStatusId = signalStatusId;
        Title = title;
        TriggerValue = triggerValue;
        ThresholdValue = thresholdValue;
        TriggeredAt = triggeredAt;
        Description = description;
    }
}
