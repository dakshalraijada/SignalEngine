namespace SignalEngine.Application.Common.Interfaces;

/// <summary>
/// Provides access to the current tenant context for query filtering.
/// This is used by EF Core global query filters to enforce tenant isolation.
/// </summary>
public interface ITenantAccessor
{
    /// <summary>
    /// Gets the current tenant ID. Returns null for system-level operations
    /// that should bypass tenant filtering (e.g., background workers processing all tenants).
    /// </summary>
    int? CurrentTenantId { get; }

    /// <summary>
    /// Indicates whether tenant filtering should be applied.
    /// When false, all tenant data is accessible (for admin/system operations).
    /// </summary>
    bool IsFilteringEnabled { get; }
}
