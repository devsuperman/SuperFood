using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperFood.Api.Common;
using SuperFood.Api.Common.Exceptions;
using SuperFood.Contracts.Menu;
using SuperFood.Domain;
using SuperFood.Infrastructure.Persistence;

namespace SuperFood.Api.Features.Menu.Categories;

/// <summary>US-0401: rename a category.</summary>
public record UpdateCategoryCommand(Guid RestaurantId, Guid CategoryId, string Name) : IRequest;

public class UpdateCategoryValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryValidator() => RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
}

public class UpdateCategoryHandler(SuperFoodDbContext db) : IRequestHandler<UpdateCategoryCommand>
{
    public async Task Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await db.Categories
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId && c.RestaurantId == request.RestaurantId, cancellationToken)
            ?? throw new NotFoundException($"Category '{request.CategoryId}' was not found.");

        category.Name = request.Name;
        await db.SaveChangesAsync(cancellationToken);
    }
}

public class UpdateCategoryEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/menu/categories/{categoryId:guid}",
            async (Guid categoryId, UpdateCategoryRequest request, HttpContext http, ISender sender, CancellationToken ct) =>
            {
                var restaurantId = http.User.GetRestaurantId()
                    ?? throw new UnauthorizedAccessException("No restaurant context.");
                await sender.Send(new UpdateCategoryCommand(restaurantId, categoryId, request.Name), ct);
                return Results.NoContent();
            })
        .WithName("UpdateCategory")
        .WithTags("Menu")
        .RequirePermission(Permissions.MenuManage);
    }
}
