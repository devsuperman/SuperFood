using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperFood.Api.Common;
using SuperFood.Api.Common.Exceptions;
using SuperFood.Contracts.Menu;
using SuperFood.Domain;
using SuperFood.Domain.Entities;
using SuperFood.Infrastructure.Persistence;

namespace SuperFood.Api.Features.Menu.Products;

/// <summary>US-0501/US-0502: create a product, optionally with variations.</summary>
public record CreateProductCommand(
    Guid RestaurantId, Guid CategoryId, string Name, string? Description, decimal Price, string? PhotoUrl,
    List<ProductVariationDto> Variations) : IRequest<ProductResponse>;

public class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
    }
}

public class CreateProductHandler(SuperFoodDbContext db) : IRequestHandler<CreateProductCommand, ProductResponse>
{
    public async Task<ProductResponse> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var categoryExists = await db.Categories
            .AnyAsync(c => c.Id == request.CategoryId && c.RestaurantId == request.RestaurantId, cancellationToken);
        if (!categoryExists)
        {
            throw new NotFoundException($"Category '{request.CategoryId}' was not found.");
        }

        var nextSortOrder = await db.Products
            .Where(p => p.CategoryId == request.CategoryId)
            .Select(p => (int?)p.SortOrder)
            .MaxAsync(cancellationToken) ?? -1;

        var product = new Product
        {
            Id = Guid.NewGuid(),
            RestaurantId = request.RestaurantId,
            CategoryId = request.CategoryId,
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            PhotoUrl = request.PhotoUrl,
            SortOrder = nextSortOrder + 1,
            Variations = request.Variations.Select(v => new ProductVariation
            {
                Id = Guid.NewGuid(),
                RestaurantId = request.RestaurantId,
                Name = v.Name,
                PriceAdjustment = v.PriceAdjustment
            }).ToList()
        };

        db.Products.Add(product);
        await db.SaveChangesAsync(cancellationToken);

        return ToResponse(product);
    }

    internal static ProductResponse ToResponse(Product p) => new(
        p.Id, p.CategoryId, p.Name, p.Description, p.Price, p.PhotoUrl, p.IsAvailable, p.IsActive, p.SortOrder,
        p.Variations.Select(v => new ProductVariationDto(v.Id, v.Name, v.PriceAdjustment)).ToList());
}

public class CreateProductEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/menu/products",
            async (CreateProductRequest request, HttpContext http, ISender sender, CancellationToken ct) =>
            {
                var restaurantId = http.User.GetRestaurantId()
                    ?? throw new UnauthorizedAccessException("No restaurant context.");
                var result = await sender.Send(new CreateProductCommand(
                    restaurantId, request.CategoryId, request.Name, request.Description, request.Price,
                    request.PhotoUrl, request.Variations), ct);
                return Results.Created($"/api/menu/products/{result.Id}", result);
            })
        .WithName("CreateProduct")
        .WithTags("Menu")
        .RequirePermission(Permissions.MenuManage);
    }
}
