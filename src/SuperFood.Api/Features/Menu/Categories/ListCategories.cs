using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperFood.Api.Common;
using SuperFood.Contracts.Menu;
using SuperFood.Infrastructure.Persistence;

namespace SuperFood.Api.Features.Menu.Categories;

public record ListCategoriesQuery(Guid RestaurantId) : IRequest<List<CategoryResponse>>;

public class ListCategoriesHandler(SuperFoodDbContext db) : IRequestHandler<ListCategoriesQuery, List<CategoryResponse>>
{
    public Task<List<CategoryResponse>> Handle(ListCategoriesQuery request, CancellationToken cancellationToken) =>
        db.Categories
            .Where(c => c.RestaurantId == request.RestaurantId)
            .OrderBy(c => c.SortOrder)
            .Select(c => new CategoryResponse(c.Id, c.Name, c.SortOrder, c.IsVisible))
            .ToListAsync(cancellationToken);
}

public class ListCategoriesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/menu/categories", async (HttpContext http, ISender sender, CancellationToken ct) =>
        {
            var restaurantId = http.User.GetRestaurantId()
                ?? throw new UnauthorizedAccessException("No restaurant context.");
            return Results.Ok(await sender.Send(new ListCategoriesQuery(restaurantId), ct));
        })
        .WithName("ListCategories")
        .WithTags("Menu")
        .RequireAuthorization();
    }
}
