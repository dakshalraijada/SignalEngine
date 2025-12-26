using Microsoft.AspNetCore.Identity;

namespace SignalEngine.Infrastructure.Identity;

/// <summary>
/// Application role.
/// </summary>
public class ApplicationRole : IdentityRole
{
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
