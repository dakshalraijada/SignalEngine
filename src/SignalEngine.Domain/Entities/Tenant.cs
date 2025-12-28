using SignalEngine.Domain.Common;

namespace SignalEngine.Domain.Entities;

/// <summary>
/// Represents a tenant in the system.
/// TenantTypeId references LookupValues (TENANT_TYPE: B2C, B2B).
/// </summary>
public class Tenant : AuditableEntity
{
    public string Name { get; private set; } = null!;
    public string Subdomain { get; private set; } = null!;
    public int TenantTypeId { get; private set; }
    public int PlanId { get; private set; }
    public bool IsActive { get; private set; }
    
    /// <summary>
    /// Default email address for notifications. If null/empty, email notifications will not be sent.
    /// </summary>
    public string? DefaultNotificationEmail { get; private set; }

    // Navigation properties
    public LookupValue? TenantType { get; private set; }
    public Plan? Plan { get; private set; }

    private readonly List<Asset> _assets = new();
    public IReadOnlyCollection<Asset> Assets => _assets.AsReadOnly();

    private readonly List<Rule> _rules = new();
    public IReadOnlyCollection<Rule> Rules => _rules.AsReadOnly();

    private Tenant() { } // EF Core

    public Tenant(string name, string subdomain, int tenantTypeId, int planId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tenant name is required.", nameof(name));

        if (string.IsNullOrWhiteSpace(subdomain))
            throw new ArgumentException("Subdomain is required.", nameof(subdomain));

        if (tenantTypeId <= 0)
            throw new ArgumentException("Tenant type ID must be positive.", nameof(tenantTypeId));

        if (planId <= 0)
            throw new ArgumentException("Plan ID must be positive.", nameof(planId));

        Name = name;
        Subdomain = subdomain.ToLowerInvariant();
        TenantTypeId = tenantTypeId;
        PlanId = planId;
        IsActive = true;
    }

    public void ChangePlan(int planId)
    {
        if (planId <= 0)
            throw new ArgumentException("Plan ID must be positive.", nameof(planId));

        PlanId = planId;
    }

    public void Update(string name, string subdomain)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tenant name is required.", nameof(name));

        if (string.IsNullOrWhiteSpace(subdomain))
            throw new ArgumentException("Subdomain is required.", nameof(subdomain));

        Name = name;
        Subdomain = subdomain.ToLowerInvariant();
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    /// <summary>
    /// Sets the default notification email for the tenant.
    /// </summary>
    /// <param name="email">The email address, or null to disable email notifications.</param>
    public void SetDefaultNotificationEmail(string? email)
    {
        DefaultNotificationEmail = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
    }
}
