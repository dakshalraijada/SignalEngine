namespace SignalEngine.Application.Common.Interfaces;

/// <summary>
/// Interface for the current user service.
/// </summary>
public interface ICurrentUserService
{
    int? UserId { get; }
    int? TenantId { get; }
    string? UserName { get; }
    bool IsAuthenticated { get; }
}
