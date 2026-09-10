using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperFood.Api.Common;
using SuperFood.Api.Common.Exceptions;
using SuperFood.Contracts.Menu;
using SuperFood.Domain;
using SuperFood.Infrastructure.Persistence;

namespace SuperFood.Api.Features.Menu.Products;

/// <summary>US-0504: activate/deactivate a product without deleting it.</summary>
public record SetProductActiveCommand(Guid RestaurantId, Guid ProductId, bool IsActive) : IRequest;

public class SetProductActiveHandler(SuperFoodDbContext db) : IRequestHandler<SetProductActiveCommand>
{
    public async Task Handle(SetProductActiveCommand request, CancellationToken cancellationToken)
    {
        var product = await db.Products
            .FirstOrDefaultAsync(p => p.Id == request.ProductId && p.RestaurantId == request.RestaurantId, cancellationToken)
            ?? throw new NotFoundException($"Product '{request.ProductId}' was not found.");

        product.IsActive = request.IsActive;
        await db.SaveChangesAsync(cancellationToken);
    }
}

public class SetProductActiveEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/menu/products/{productId:guid}/active",
            async (Guid productId, SetProductActiveRequest request, HttpContext http, ISender sender, CancellationToken ct) =>
            {
                var restaurantId = http.User.GetRestaurantId()
                    ?? throw new UnauthorizedAccessException("No restaurant context.");
                await sender.Send(new SetProductActiveCommand(restaurantId, productId, request.IsActive), ct);
                return Results.NoContent();
            })
        .WithName("SetProductActive")
        .WithTags("Menu")
        .RequirePermission(Permissions.MenuManage);
    }
}
