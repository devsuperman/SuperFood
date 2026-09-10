using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperFood.Api.Common;
using SuperFood.Contracts.Menu;
using SuperFood.Domain;
using SuperFood.Infrastructure.Persistence;

namespace SuperFood.Api.Features.Menu.Categories;

/// <summary>US-0402: reorder categories on the menu.</summary>
public record ReorderCategoriesCommand(Guid RestaurantId, List<Guid> OrderedCategoryIds) : IRequest;

public class ReorderCategoriesHandler(SuperFoodDbContext db) : IRequestHandler<ReorderCategoriesCommand>
{
    public async Task Handle(ReorderCategoriesCommand request, CancellationToken cancellationToken)
    {
        var categories = await db.Categories
            .Where(c => c.RestaurantId == request.RestaurantId)
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        for (var i = 0; i < request.OrderedCategoryIds.Count; i++)
        {
            if (categories.TryGetValue(request.OrderedCategoryIds[i], out var category))
            {
                category.SortOrder = i;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}

public class ReorderCategoriesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/menu/categories/order",
            async (ReorderCategoriesRequest request, HttpContext http, ISender sender, CancellationToken ct) =>
            {
                var restaurantId = http.User.GetRestaurantId()
                    ?? throw new UnauthorizedAccessException("No restaurant context.");
                await sender.Send(new ReorderCategoriesCommand(restaurantId, request.OrderedCategoryIds), ct);
                return Results.NoContent();
            })
        .WithName("ReorderCategories")
        .WithTags("Menu")
        .RequirePermission(Permissions.MenuManage);
    }
}
