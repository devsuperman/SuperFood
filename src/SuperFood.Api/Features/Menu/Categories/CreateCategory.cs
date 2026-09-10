using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperFood.Api.Common;
using SuperFood.Contracts.Menu;
using SuperFood.Domain;
using SuperFood.Domain.Entities;
using SuperFood.Infrastructure.Persistence;

namespace SuperFood.Api.Features.Menu.Categories;

/// <summary>US-0401: create a menu category.</summary>
public record CreateCategoryCommand(Guid RestaurantId, string Name) : IRequest<CategoryResponse>;

public class CreateCategoryValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryValidator() => RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
}

public class CreateCategoryHandler(SuperFoodDbContext db) : IRequestHandler<CreateCategoryCommand, CategoryResponse>
{
    public async Task<CategoryResponse> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var nextSortOrder = await db.Categories
            .Where(c => c.RestaurantId == request.RestaurantId)
            .Select(c => (int?)c.SortOrder)
            .MaxAsync(cancellationToken) ?? -1;

        var category = new Category
        {
            Id = Guid.NewGuid(),
            RestaurantId = request.RestaurantId,
            Name = request.Name,
            SortOrder = nextSortOrder + 1,
            IsVisible = true
        };

        db.Categories.Add(category);
        await db.SaveChangesAsync(cancellationToken);

        return new CategoryResponse(category.Id, category.Name, category.SortOrder, category.IsVisible);
    }
}

public class CreateCategoryEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/menu/categories",
            async (CreateCategoryRequest request, HttpContext http, ISender sender, CancellationToken ct) =>
            {
                var restaurantId = http.User.GetRestaurantId()
                    ?? throw new UnauthorizedAccessException("No restaurant context.");
                var result = await sender.Send(new CreateCategoryCommand(restaurantId, request.Name), ct);
                return Results.Created($"/api/menu/categories/{result.Id}", result);
            })
        .WithName("CreateCategory")
        .WithTags("Menu")
        .RequirePermission(Permissions.MenuManage);
    }
}
