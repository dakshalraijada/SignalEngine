using SignalEngine.Domain.Common;

namespace SignalEngine.Domain.Entities;

/// <summary>
/// Represents a subscription plan.
/// PlanCodeId references LookupValues (PLAN_CODE: FREE, PRO, BUSINESS).
/// </summary>
public class Plan : AuditableEntity
{
    public string Name { get; private set; } = null!;
    public int PlanCodeId { get; private set; }
    public int MaxRules { get; private set; }
    public int MaxAssets { get; private set; }
    public int MaxNotificationsPerDay { get; private set; }
    public bool AllowWebhook { get; private set; }
    public bool AllowSlack { get; private set; }
    public decimal MonthlyPrice { get; private set; }
    public bool IsActive { get; private set; }

    // Navigation properties
    public LookupValue? PlanCode { get; private set; }

    private Plan() { } // EF Core

    public Plan(
        string name,
        int planCodeId,
        int maxRules,
        int maxAssets,
        int maxNotificationsPerDay,
        bool allowWebhook,
        bool allowSlack,
        decimal monthlyPrice)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Plan name is required.", nameof(name));

        if (planCodeId <= 0)
            throw new ArgumentException("Plan code ID must be positive.", nameof(planCodeId));

        if (maxRules < 0)
            throw new ArgumentException("Max rules cannot be negative.", nameof(maxRules));

        if (maxAssets < 0)
            throw new ArgumentException("Max assets cannot be negative.", nameof(maxAssets));

        if (maxNotificationsPerDay < 0)
            throw new ArgumentException("Max notifications per day cannot be negative.", nameof(maxNotificationsPerDay));

        if (monthlyPrice < 0)
            throw new ArgumentException("Monthly price cannot be negative.", nameof(monthlyPrice));

        Name = name;
        PlanCodeId = planCodeId;
        MaxRules = maxRules;
        MaxAssets = maxAssets;
        MaxNotificationsPerDay = maxNotificationsPerDay;
        AllowWebhook = allowWebhook;
        AllowSlack = allowSlack;
        MonthlyPrice = monthlyPrice;
        IsActive = true;
    }

    public void Update(
        string name,
        int maxRules,
        int maxAssets,
        int maxNotificationsPerDay,
        bool allowWebhook,
        bool allowSlack,
        decimal monthlyPrice)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Plan name is required.", nameof(name));

        Name = name;
        MaxRules = maxRules;
        MaxAssets = maxAssets;
        MaxNotificationsPerDay = maxNotificationsPerDay;
        AllowWebhook = allowWebhook;
        AllowSlack = allowSlack;
        MonthlyPrice = monthlyPrice;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
