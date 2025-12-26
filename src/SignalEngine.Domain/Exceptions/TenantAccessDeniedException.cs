namespace SignalEngine.Domain.Exceptions;

/// <summary>
/// Exception thrown when tenant access is denied.
/// </summary>
public class TenantAccessDeniedException : DomainException
{
    public int TenantId { get; }
    public int RequestedTenantId { get; }

    public TenantAccessDeniedException(int tenantId, int requestedTenantId)
        : base($"Access denied. User belongs to tenant {tenantId} but requested access to tenant {requestedTenantId}.")
    {
        TenantId = tenantId;
        RequestedTenantId = requestedTenantId;
    }
}
