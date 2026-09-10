using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperFood.Api.Common;
using SuperFood.Api.Common.Exceptions;
using SuperFood.Infrastructure.Persistence;

namespace SuperFood.Api.Features.Restaurants;

/// <summary>
/// US-0105: permanently deactivate a restaurant. Confirmation is a client-side
/// concern (destructive action); the backend just marks it suspended and
/// stops serving its public pages. Full hard-delete is left to a future
/// data-retention story rather than cascading through every child table here.
/// </summary>
public record DeleteRestaurantCommand(Guid RestaurantId) : IRequest;

public class DeleteRestaurantHandler(SuperFoodDbContext db) : IRequestHandler<DeleteRestaurantCommand>
{
    public async Task Handle(DeleteRestaurantCommand request, CancellationToken cancellationToken)
    {
        var restaurant = await db.Restaurants.FirstOrDefaultAsync(r => r.Id == request.RestaurantId, cancellationToken)
            ?? throw new NotFoundException($"Restaurant '{request.RestaurantId}' was not found.");

        restaurant.Status = Domain.Enums.RestaurantStatus.Suspended;
        restaurant.DineInEnabled = false;
        restaurant.DeliveryEnabled = false;
        await db.SaveChangesAsync(cancellationToken);
    }
}

public class DeleteRestaurantEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/restaurants/{restaurantId:guid}", async (Guid restaurantId, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new DeleteRestaurantCommand(restaurantId), ct);
            return Results.NoContent();
        })
        .WithName("DeleteRestaurant")
        .WithTags("Restaurants")
        .RequirePlatformAdmin();
    }
}
