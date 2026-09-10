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

/// <summary>US-0501/US-0502: update a product's fields and replace its variation set.</summary>
public record UpdateProductCommand(
    Guid RestaurantId, Guid ProductId, Guid CategoryId, string Name, string? Description, decimal Price,
    string? PhotoUrl, List<ProductVariationDto> Variations) : IRequest;

public class UpdateProductValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
    }
}

public class UpdateProductHandler(SuperFoodDbContext db) : IRequestHandler<UpdateProductCommand>
{
    public async Task Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await db.Products
            .Include(p => p.Variations)
            .FirstOrDefaultAsync(p => p.Id == request.ProductId && p.RestaurantId == request.RestaurantId, cancellationToken)
            ?? throw new NotFoundException($"Product '{request.ProductId}' was not found.");

        var categoryExists = await db.Categories
            .AnyAsync(c => c.Id == request.CategoryId && c.RestaurantId == request.RestaurantId, cancellationToken);
        if (!categoryExists)
        {
            throw new NotFoundException($"Category '{request.CategoryId}' was not found.");
        }

        product.CategoryId = request.CategoryId;
        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;
        product.PhotoUrl = request.PhotoUrl;

        product.Variations.Clear();
        foreach (var v in request.Variations)
        {
            product.Variations.Add(new ProductVariation
            {
                Id = v.Id ?? Guid.NewGuid(),
                RestaurantId = request.RestaurantId,
                Name = v.Name,
                PriceAdjustment = v.PriceAdjustment
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}

public class UpdateProductEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/menu/products/{productId:guid}",
            async (Guid productId, UpdateProductRequest request, HttpContext http, ISender sender, CancellationToken ct) =>
            {
                var restaurantId = http.User.GetRestaurantId()
                    ?? throw new UnauthorizedAccessException("No restaurant context.");
                await sender.Send(new UpdateProductCommand(
                    restaurantId, productId, request.CategoryId, request.Name, request.Description, request.Price,
                    request.PhotoUrl, request.Variations), ct);
                return Results.NoContent();
            })
        .WithName("UpdateProduct")
        .WithTags("Menu")
        .RequirePermission(Permissions.MenuManage);
    }
}
