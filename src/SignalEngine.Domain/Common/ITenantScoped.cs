namespace SignalEngine.Domain.Common;

/// <summary>
/// Interface for entities that belong to a tenant.
/// </summary>
public interface ITenantScoped
{
    int TenantId { get; }
}
