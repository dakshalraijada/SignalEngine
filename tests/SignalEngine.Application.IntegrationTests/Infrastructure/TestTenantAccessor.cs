using SignalEngine.Application.Common.Interfaces;

namespace SignalEngine.Application.IntegrationTests.Infrastructure;

/// <summary>
/// Test implementation of ITenantAccessor for controlling tenant context in tests.
/// Supports both tenant-scoped and system-level (bypass filtering) modes.
/// </summary>
public class TestTenantAccessor : ITenantAccessor
{
    private readonly int? _tenantId;
    private readonly bool _isFilteringEnabled;

    private TestTenantAccessor(int? tenantId, bool isFilteringEnabled)
    {
        _tenantId = tenantId;
        _isFilteringEnabled = isFilteringEnabled;
    }

    public int? CurrentTenantId => _tenantId;
    public bool IsFilteringEnabled => _isFilteringEnabled;

    /// <summary>
    /// Creates a tenant accessor that filters to a specific tenant.
    /// Use this to simulate API requests from a specific tenant.
    /// </summary>
    public static TestTenantAccessor ForTenant(int tenantId) 
        => new(tenantId, isFilteringEnabled: true);

    /// <summary>
    /// Creates a system-level accessor that bypasses tenant filtering.
    /// Use this for system operations like background workers.
    /// </summary>
    public static TestTenantAccessor SystemLevel() 
        => new(tenantId: null, isFilteringEnabled: false);
}
