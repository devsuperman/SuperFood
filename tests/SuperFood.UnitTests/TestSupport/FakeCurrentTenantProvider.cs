using SuperFood.Infrastructure.Persistence;

namespace SuperFood.UnitTests.TestSupport;

public class FakeCurrentTenantProvider(Guid? restaurantId, bool isPlatformAdmin = false) : ICurrentTenantProvider
{
    public Guid? RestaurantId { get; } = restaurantId;
    public bool IsPlatformAdmin { get; } = isPlatformAdmin;
}
