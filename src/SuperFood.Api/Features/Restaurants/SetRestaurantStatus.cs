using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperFood.Api.Common;
using SuperFood.Api.Common.Exceptions;
using SuperFood.Domain.Enums;
using SuperFood.Infrastructure.Persistence;

namespace SuperFood.Api.Features.Restaurants;

/// <summary>US-0103: suspend or reactivate a restaurant without deleting data.</summary>
public record SetRestaurantStatusCommand(Guid RestaurantId, RestaurantStatus Status) : IRequest;

public class SetRestaurantStatusHandler(SuperFoodDbContext db) : IRequestHandler<SetRestaurantStatusCommand>
{
    public async Task Handle(SetRestaurantStatusCommand request, CancellationToken cancellationToken)
    {
        var restaurant = await db.Restaurants.FirstOrDefaultAsync(r => r.Id == request.RestaurantId, cancellationToken)
            ?? throw new NotFoundException($"Restaurant '{request.RestaurantId}' was not found.");

        restaurant.Status = request.Status;
        await db.SaveChangesAsync(cancellationToken);
    }
}

public class SetRestaurantStatusEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/restaurants/{restaurantId:guid}/suspend", async (Guid restaurantId, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new SetRestaurantStatusCommand(restaurantId, RestaurantStatus.Suspended), ct);
            return Results.NoContent();
        })
        .WithName("SuspendRestaurant")
        .WithTags("Restaurants")
        .RequirePlatformAdmin();

        app.MapPost("/api/restaurants/{restaurantId:guid}/reactivate", async (Guid restaurantId, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new SetRestaurantStatusCommand(restaurantId, RestaurantStatus.Active), ct);
            return Results.NoContent();
        })
        .WithName("ReactivateRestaurant")
        .WithTags("Restaurants")
        .RequirePlatformAdmin();
    }
}
