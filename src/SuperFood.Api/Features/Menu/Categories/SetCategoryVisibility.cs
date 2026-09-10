using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperFood.Api.Common;
using SuperFood.Api.Common.Exceptions;
using SuperFood.Contracts.Menu;
using SuperFood.Domain;
using SuperFood.Infrastructure.Persistence;

namespace SuperFood.Api.Features.Menu.Categories;

/// <summary>US-0403: temporarily hide a category without deleting it.</summary>
public record SetCategoryVisibilityCommand(Guid RestaurantId, Guid CategoryId, bool IsVisible) : IRequest;

public class SetCategoryVisibilityHandler(SuperFoodDbContext db) : IRequestHandler<SetCategoryVisibilityCommand>
{
    public async Task Handle(SetCategoryVisibilityCommand request, CancellationToken cancellationToken)
    {
        var category = await db.Categories
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId && c.RestaurantId == request.RestaurantId, cancellationToken)
            ?? throw new NotFoundException($"Category '{request.CategoryId}' was not found.");

        category.IsVisible = request.IsVisible;
        await db.SaveChangesAsync(cancellationToken);
    }
}

public class SetCategoryVisibilityEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/menu/categories/{categoryId:guid}/visibility",
            async (Guid categoryId, SetCategoryVisibilityRequest request, HttpContext http, ISender sender, CancellationToken ct) =>
            {
                var restaurantId = http.User.GetRestaurantId()
                    ?? throw new UnauthorizedAccessException("No restaurant context.");
                await sender.Send(new SetCategoryVisibilityCommand(restaurantId, categoryId, request.IsVisible), ct);
                return Results.NoContent();
            })
        .WithName("SetCategoryVisibility")
        .WithTags("Menu")
        .RequirePermission(Permissions.MenuManage);
    }
}
