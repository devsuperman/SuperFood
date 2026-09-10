using System.Security.Claims;

namespace SuperFood.Api.Common;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub")
            ?? throw new InvalidOperationException("No user id claim present."));

    public static Guid? GetRestaurantId(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue("restaurant_id"), out var id) ? id : null;

    public static bool IsPlatformAdmin(this ClaimsPrincipal user) =>
        user.FindFirstValue("is_platform_admin") == "true";
}
