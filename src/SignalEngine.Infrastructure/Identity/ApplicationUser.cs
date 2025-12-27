using Microsoft.AspNetCore.Identity;
using SignalEngine.Domain.Entities;

namespace SignalEngine.Infrastructure.Identity;

/// <summary>
/// Application user with tenant association.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public int TenantId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation property
    public Tenant? Tenant { get; set; }
}
