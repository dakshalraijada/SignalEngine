using SignalEngine.Application.Common.Interfaces;

namespace SignalEngine.Worker.Services;

/// <summary>
/// Current user service implementation for background workers.
/// Returns null values as there is no authenticated user context.
/// The TenantAccessor will see IsFilteringEnabled = false because TenantId is null,
/// allowing the worker to process data across all tenants.
/// </summary>
public class SystemCurrentUserService : ICurrentUserService
{
    public int? UserId => null;
    public int? TenantId => null;
    public string? UserName => "System";
    public bool IsAuthenticated => false;
}
