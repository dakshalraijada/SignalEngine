using SignalEngine.Domain.Common;

namespace SignalEngine.Domain.Entities;

/// <summary>
/// Tracks the state of a rule evaluation for detecting consecutive breaches.
/// </summary>
public class SignalState : AuditableEntity, ITenantScoped
{
    public int TenantId { get; private set; }
    public int RuleId { get; private set; }
    public int ConsecutiveBreaches { get; private set; }
    public DateTime LastEvaluatedAt { get; private set; }
    public decimal? LastMetricValue { get; private set; }
    public bool IsBreached { get; private set; }

    private SignalState() { } // EF Core

    public SignalState(int tenantId, int ruleId)
    {
        if (tenantId <= 0)
            throw new ArgumentException("Tenant ID must be positive.", nameof(tenantId));

        if (ruleId <= 0)
            throw new ArgumentException("Rule ID must be positive.", nameof(ruleId));

        TenantId = tenantId;
        RuleId = ruleId;
        ConsecutiveBreaches = 0;
        LastEvaluatedAt = DateTime.UtcNow;
        IsBreached = false;
    }

    public void RecordBreach(decimal metricValue)
    {
        ConsecutiveBreaches++;
        LastMetricValue = metricValue;
        LastEvaluatedAt = DateTime.UtcNow;
        IsBreached = true;
    }

    public void RecordSuccess(decimal metricValue)
    {
        ConsecutiveBreaches = 0;
        LastMetricValue = metricValue;
        LastEvaluatedAt = DateTime.UtcNow;
        IsBreached = false;
    }

    public void Reset()
    {
        ConsecutiveBreaches = 0;
        LastMetricValue = null;
        IsBreached = false;
        LastEvaluatedAt = DateTime.UtcNow;
    }
}
