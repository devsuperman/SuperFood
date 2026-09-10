using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperFood.Api.Common;
using SuperFood.Api.Common.Exceptions;
using SuperFood.Contracts.Menu;
using SuperFood.Domain;
using SuperFood.Infrastructure.Persistence;

namespace SuperFood.Api.Features.Menu.Products;

/// <summary>US-0503: mark a product unavailable/out of stock without deleting it.</summary>
public record SetProductAvailabilityCommand(Guid RestaurantId, Guid ProductId, bool IsAvailable) : IRequest;

public class SetProductAvailabilityHandler(SuperFoodDbContext db) : IRequestHandler<SetProductAvailabilityCommand>
{
    public async Task Handle(SetProductAvailabilityCommand request, CancellationToken cancellationToken)
    {
        var product = await db.Products
            .FirstOrDefaultAsync(p => p.Id == request.ProductId && p.RestaurantId == request.RestaurantId, cancellationToken)
            ?? throw new NotFoundException($"Product '{request.ProductId}' was not found.");

        product.IsAvailable = request.IsAvailable;
        await db.SaveChangesAsync(cancellationToken);
    }
}

public class SetProductAvailabilityEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/menu/products/{productId:guid}/availability",
            async (Guid productId, SetProductAvailabilityRequest request, HttpContext http, ISender sender, CancellationToken ct) =>
            {
                var restaurantId = http.User.GetRestaurantId()
                    ?? throw new UnauthorizedAccessException("No restaurant context.");
                await sender.Send(new SetProductAvailabilityCommand(restaurantId, productId, request.IsAvailable), ct);
                return Results.NoContent();
            })
        .WithName("SetProductAvailability")
        .WithTags("Menu")
        .RequirePermission(Permissions.MenuManage);
    }
}
