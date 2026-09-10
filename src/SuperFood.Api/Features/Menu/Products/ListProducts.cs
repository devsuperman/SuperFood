using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperFood.Api.Common;
using SuperFood.Contracts.Menu;
using SuperFood.Infrastructure.Persistence;

namespace SuperFood.Api.Features.Menu.Products;

/// <summary>Backs the product management screen (EPIC-05), optionally scoped to one category.</summary>
public record ListProductsQuery(Guid RestaurantId, Guid? CategoryId) : IRequest<List<ProductResponse>>;

public class ListProductsHandler(SuperFoodDbContext db) : IRequestHandler<ListProductsQuery, List<ProductResponse>>
{
    public async Task<List<ProductResponse>> Handle(ListProductsQuery request, CancellationToken cancellationToken)
    {
        var query = db.Products.Include(p => p.Variations)
            .Where(p => p.RestaurantId == request.RestaurantId);

        if (request.CategoryId is { } categoryId)
        {
            query = query.Where(p => p.CategoryId == categoryId);
        }

        var products = await query.OrderBy(p => p.SortOrder).ToListAsync(cancellationToken);
        return products.Select(CreateProductHandler.ToResponse).ToList();
    }
}

public class ListProductsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/menu/products", async (Guid? categoryId, HttpContext http, ISender sender, CancellationToken ct) =>
        {
            var restaurantId = http.User.GetRestaurantId()
                ?? throw new UnauthorizedAccessException("No restaurant context.");
            return Results.Ok(await sender.Send(new ListProductsQuery(restaurantId, categoryId), ct));
        })
        .WithName("ListProducts")
        .WithTags("Menu")
        .RequireAuthorization();
    }
}
