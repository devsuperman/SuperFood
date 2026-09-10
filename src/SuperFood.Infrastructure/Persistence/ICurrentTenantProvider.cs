namespace SuperFood.Infrastructure.Persistence;

/// <summary>
/// Resolves the current request's tenant (restaurant) from the JWT `restaurant_id`
/// claim, backing the EF Core global query filters (docs/tech-stack.md §4).
/// </summary>
public interface ICurrentTenantProvider
{
    Guid? RestaurantId { get; }
    bool IsPlatformAdmin { get; }
}
