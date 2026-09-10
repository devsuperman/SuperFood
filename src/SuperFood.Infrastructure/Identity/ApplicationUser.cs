using Microsoft.AspNetCore.Identity;

namespace SuperFood.Infrastructure.Identity;

/// <summary>
/// Null <see cref="RestaurantId"/> + <see cref="IsPlatformAdmin"/> = platform_admin (EPIC-01).
/// Otherwise a restaurant staff member with a <see cref="RoleId"/> granting permissions (US-0303).
/// Customers are never represented here (see docs/tech-stack.md §5).
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsPlatformAdmin { get; set; }
    public Guid? RestaurantId { get; set; }
    public Guid? RoleId { get; set; }
}
