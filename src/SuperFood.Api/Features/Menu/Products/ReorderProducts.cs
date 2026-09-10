using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperFood.Api.Common;
using SuperFood.Contracts.Menu;
using SuperFood.Domain;
using SuperFood.Infrastructure.Persistence;

namespace SuperFood.Api.Features.Menu.Products;

/// <summary>US-0505: reorder products within a category.</summary>
public record ReorderProductsCommand(Guid RestaurantId, List<Guid> OrderedProductIds) : IRequest;

public class ReorderProductsHandler(SuperFoodDbContext db) : IRequestHandler<ReorderProductsCommand>
{
    public async Task Handle(ReorderProductsCommand request, CancellationToken cancellationToken)
    {
        var products = await db.Products
            .Where(p => p.RestaurantId == request.RestaurantId)
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        for (var i = 0; i < request.OrderedProductIds.Count; i++)
        {
            if (products.TryGetValue(request.OrderedProductIds[i], out var product))
            {
                product.SortOrder = i;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}

public class ReorderProductsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/menu/products/order",
            async (ReorderProductsRequest request, HttpContext http, ISender sender, CancellationToken ct) =>
            {
                var restaurantId = http.User.GetRestaurantId()
                    ?? throw new UnauthorizedAccessException("No restaurant context.");
                await sender.Send(new ReorderProductsCommand(restaurantId, request.OrderedProductIds), ct);
                return Results.NoContent();
            })
        .WithName("ReorderProducts")
        .WithTags("Menu")
        .RequirePermission(Permissions.MenuManage);
    }
}
