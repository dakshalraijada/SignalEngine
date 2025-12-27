using SignalEngine.Domain.Common;

namespace SignalEngine.Domain.Entities;

/// <summary>
/// Represents the resolution of a signal.
/// This follows the append-only pattern - signals are immutable, resolutions are separate records.
/// </summary>
public class SignalResolution : AuditableEntity, ITenantScoped
{
    public int TenantId { get; private set; }
    public int SignalId { get; private set; }
    public int ResolutionStatusId { get; private set; }
    public int ResolvedByUserId { get; private set; }
    public string? Notes { get; private set; }
    public DateTime ResolvedAt { get; private set; }

    // Navigation properties
    public Signal Signal { get; private set; } = null!;
    public LookupValue ResolutionStatus { get; private set; } = null!;
    public Tenant Tenant { get; private set; } = null!;

    private SignalResolution() { } // EF Core

    public SignalResolution(
        int tenantId,
        int signalId,
        int resolutionStatusId,
        int resolvedByUserId,
        string? notes = null)
    {
        if (tenantId <= 0)
            throw new ArgumentException("Tenant ID must be positive.", nameof(tenantId));

        if (signalId <= 0)
            throw new ArgumentException("Signal ID must be positive.", nameof(signalId));

        if (resolutionStatusId <= 0)
            throw new ArgumentException("Resolution status ID must be positive.", nameof(resolutionStatusId));

        if (resolvedByUserId <= 0)
            throw new ArgumentException("Resolved by user ID must be positive.", nameof(resolvedByUserId));

        TenantId = tenantId;
        SignalId = signalId;
        ResolutionStatusId = resolutionStatusId;
        ResolvedByUserId = resolvedByUserId;
        Notes = notes;
        ResolvedAt = DateTime.UtcNow;
    }
}
