using Microsoft.EntityFrameworkCore;
using SuperFood.Api.Common.Exceptions;
using SuperFood.Domain.Entities;
using SuperFood.Infrastructure.Persistence;

namespace SuperFood.Api.Features.Orders;

/// <summary>US-0202 AC: block ordering outside operating hours or a disabled order type.</summary>
internal static class OrderingWindowGuard
{
    public static async Task<Restaurant> EnsureCanOrderAsync(
        SuperFoodDbContext db, Guid restaurantId, bool dineIn, CancellationToken cancellationToken)
    {
        var restaurant = await db.Restaurants.IgnoreQueryFilters()
            .Include(r => r.OperatingHours)
            .FirstOrDefaultAsync(r => r.Id == restaurantId, cancellationToken)
            ?? throw new NotFoundException("Restaurant was not found.");

        if (restaurant.Status == Domain.Enums.RestaurantStatus.Suspended)
        {
            throw new ConflictException("This restaurant is not currently accepting orders.");
        }

        if (dineIn && !restaurant.DineInEnabled)
        {
            throw new ConflictException("Dine-in ordering is not enabled for this restaurant.");
        }

        if (!dineIn && !restaurant.DeliveryEnabled)
        {
            throw new ConflictException("Delivery ordering is not enabled for this restaurant.");
        }

        if (!IsOpenNow(restaurant))
        {
            throw new ConflictException("This restaurant is currently closed for ordering.");
        }

        return restaurant;
    }

    /// <summary>Non-throwing check, reused by the Catalog slices (US-1001) for display purposes.</summary>
    public static bool IsOpenNow(Restaurant restaurant)
    {
        var now = DateTimeOffset.UtcNow;
        var todayHours = restaurant.OperatingHours.FirstOrDefault(h => h.DayOfWeek == now.DayOfWeek);
        if (todayHours is null)
        {
            return true;
        }

        return !todayHours.IsClosed && IsWithin(TimeOnly.FromDateTime(now.UtcDateTime), todayHours.OpenTime, todayHours.CloseTime);
    }

    private static bool IsWithin(TimeOnly now, TimeOnly open, TimeOnly close) =>
        open <= close ? now >= open && now <= close : now >= open || now <= close;
}
