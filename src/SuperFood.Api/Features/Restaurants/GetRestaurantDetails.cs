using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperFood.Api.Common;
using SuperFood.Api.Common.Exceptions;
using SuperFood.Contracts.Restaurants;
using SuperFood.Infrastructure.Identity;
using SuperFood.Infrastructure.Persistence;

namespace SuperFood.Api.Features.Restaurants;

/// <summary>
/// US-0104 (platform_admin support view) and also used by a restaurant's own
/// staff to read their profile — the tenant query filter naturally scopes a
/// non-admin caller to their own restaurant (docs/tech-stack.md §4).
/// </summary>
public record GetRestaurantDetailsQuery(Guid RestaurantId) : IRequest<RestaurantDetailsResponse>;

public class GetRestaurantDetailsHandler(SuperFoodDbContext db)
    : IRequestHandler<GetRestaurantDetailsQuery, RestaurantDetailsResponse>
{
    public async Task<RestaurantDetailsResponse> Handle(GetRestaurantDetailsQuery request, CancellationToken cancellationToken)
    {
        var restaurant = await db.Restaurants.FirstOrDefaultAsync(r => r.Id == request.RestaurantId, cancellationToken)
            ?? throw new NotFoundException($"Restaurant '{request.RestaurantId}' was not found.");

        var staffCount = await db.Set<ApplicationUser>()
            .CountAsync(u => u.RestaurantId == restaurant.Id, cancellationToken);

        return new RestaurantDetailsResponse(
            restaurant.Id, restaurant.Name, restaurant.Status.ToString(), restaurant.CreatedAt,
            restaurant.ContactEmail, restaurant.ContactPhone, restaurant.Address, restaurant.LogoUrl,
            restaurant.DineInEnabled, restaurant.DeliveryEnabled, staffCount);
    }
}

public class GetRestaurantDetailsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/restaurants/{restaurantId:guid}", async (Guid restaurantId, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetRestaurantDetailsQuery(restaurantId), ct)))
        .WithName("GetRestaurantDetails")
        .WithTags("Restaurants")
        .RequireAuthorization();
    }
}
