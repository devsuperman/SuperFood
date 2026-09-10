using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperFood.Api.Common;
using SuperFood.Api.Common.Exceptions;
using SuperFood.Domain;
using SuperFood.Infrastructure.Persistence;

namespace SuperFood.Api.Features.Menu.Categories;

/// <summary>US-0401: delete a category (only when it has no products left).</summary>
public record DeleteCategoryCommand(Guid RestaurantId, Guid CategoryId) : IRequest;

public class DeleteCategoryHandler(SuperFoodDbContext db) : IRequestHandler<DeleteCategoryCommand>
{
    public async Task Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await db.Categories
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId && c.RestaurantId == request.RestaurantId, cancellationToken)
            ?? throw new NotFoundException($"Category '{request.CategoryId}' was not found.");

        var hasProducts = await db.Products.AnyAsync(p => p.CategoryId == category.Id, cancellationToken);
        if (hasProducts)
        {
            throw new Common.Exceptions.ConflictException(
                "This category still has products. Move or delete them first.");
        }

        db.Categories.Remove(category);
        await db.SaveChangesAsync(cancellationToken);
    }
}

public class DeleteCategoryEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/menu/categories/{categoryId:guid}",
            async (Guid categoryId, HttpContext http, ISender sender, CancellationToken ct) =>
            {
                var restaurantId = http.User.GetRestaurantId()
                    ?? throw new UnauthorizedAccessException("No restaurant context.");
                await sender.Send(new DeleteCategoryCommand(restaurantId, categoryId), ct);
                return Results.NoContent();
            })
        .WithName("DeleteCategory")
        .WithTags("Menu")
        .RequirePermission(Permissions.MenuManage);
    }
}
