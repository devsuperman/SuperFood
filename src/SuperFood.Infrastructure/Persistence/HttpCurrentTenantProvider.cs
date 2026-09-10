using Microsoft.AspNetCore.Http;

namespace SuperFood.Infrastructure.Persistence;

public class HttpCurrentTenantProvider(IHttpContextAccessor httpContextAccessor) : ICurrentTenantProvider
{
    public Guid? RestaurantId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User.FindFirst("restaurant_id")?.Value;
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public bool IsPlatformAdmin =>
        httpContextAccessor.HttpContext?.User.FindFirst("is_platform_admin")?.Value == "true";
}
