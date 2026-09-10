using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperFood.Api.Common;
using SuperFood.Api.Common.Exceptions;
using SuperFood.Contracts.Restaurants;
using SuperFood.Domain;
using SuperFood.Infrastructure.Persistence;

namespace SuperFood.Api.Features.Restaurants;

/// <summary>US-0203: enable/disable dine-in and delivery order types independently.</summary>
public record UpdateOrderTypesCommand(Guid RestaurantId, bool DineInEnabled, bool DeliveryEnabled) : IRequest;

public class UpdateOrderTypesHandler(SuperFoodDbContext db) : IRequestHandler<UpdateOrderTypesCommand>
{
    public async Task Handle(UpdateOrderTypesCommand request, CancellationToken cancellationToken)
    {
        var restaurant = await db.Restaurants.FirstOrDefaultAsync(r => r.Id == request.RestaurantId, cancellationToken)
            ?? throw new NotFoundException("Restaurant was not found.");

        restaurant.DineInEnabled = request.DineInEnabled;
        restaurant.DeliveryEnabled = request.DeliveryEnabled;
        await db.SaveChangesAsync(cancellationToken);
    }
}

public class UpdateOrderTypesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/restaurants/me/order-types",
            async (UpdateOrderTypesRequest request, HttpContext http, ISender sender, CancellationToken ct) =>
            {
                var restaurantId = http.User.GetRestaurantId()
                    ?? throw new UnauthorizedAccessException("No restaurant context.");
                await sender.Send(new UpdateOrderTypesCommand(restaurantId, request.DineInEnabled, request.DeliveryEnabled), ct);
                return Results.NoContent();
            })
        .WithName("UpdateOrderTypes")
        .WithTags("Restaurants")
        .RequirePermission(Permissions.RestaurantManage);
    }
}
