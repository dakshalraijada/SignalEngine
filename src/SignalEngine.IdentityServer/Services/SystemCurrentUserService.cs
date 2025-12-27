using SignalEngine.Application.Common.Interfaces;

namespace SignalEngine.IdentityServer.Services;

/// <summary>
/// Current user service implementation for IdentityServer.
/// Returns null values as data seeding runs without user context.
/// </summary>
public class SystemCurrentUserService : ICurrentUserService
{
    public int? UserId => null;
    public int? TenantId => null;
    public string? UserName => "System";
    public bool IsAuthenticated => false;
}
