using SignalEngine.Application.Common.Interfaces;

namespace SignalEngine.Infrastructure.Services;

/// <summary>
/// Provides the current tenant context from the authenticated user.
/// Used by EF Core global query filters to enforce tenant data isolation.
/// </summary>
public class TenantAccessor : ITenantAccessor
{
    private readonly ICurrentUserService _currentUserService;
    private int? _overrideTenantId;
    private bool _filteringDisabled;

    public TenantAccessor(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    /// <inheritdoc />
    public int? CurrentTenantId => _overrideTenantId ?? _currentUserService.TenantId;

    /// <inheritdoc />
    public bool IsFilteringEnabled => !_filteringDisabled && CurrentTenantId.HasValue;

    /// <summary>
    /// Temporarily overrides the tenant context for system operations.
    /// Use with caution - this bypasses normal tenant isolation.
    /// </summary>
    /// <param name="tenantId">The tenant ID to use, or null to disable filtering.</param>
    /// <returns>A disposable that restores the previous tenant context.</returns>
    public IDisposable OverrideTenant(int? tenantId)
    {
        var previousOverride = _overrideTenantId;
        _overrideTenantId = tenantId;
        return new TenantOverrideScope(() => _overrideTenantId = previousOverride);
    }

    /// <summary>
    /// Temporarily disables tenant filtering for system-wide operations.
    /// Use with extreme caution - this allows access to all tenant data.
    /// </summary>
    /// <returns>A disposable that re-enables filtering.</returns>
    public IDisposable DisableFiltering()
    {
        _filteringDisabled = true;
        return new TenantOverrideScope(() => _filteringDisabled = false);
    }

    private sealed class TenantOverrideScope : IDisposable
    {
        private readonly Action _restoreAction;

        public TenantOverrideScope(Action restoreAction)
        {
            _restoreAction = restoreAction;
        }

        public void Dispose() => _restoreAction();
    }
}
