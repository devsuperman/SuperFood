using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperFood.Api.Common;
using SuperFood.Api.Common.Exceptions;
using SuperFood.Api.Features.Orders;
using SuperFood.Contracts.Catalog;
using SuperFood.Domain.Entities;
using SuperFood.Infrastructure.Persistence;

namespace SuperFood.Api.Features.Catalog;

/// <summary>
/// US-1001: browse a restaurant's menu without an account. Only visible
/// categories and active products are shown; unavailable products still show
/// but disabled (US-0503), matching the Products management semantics.
/// </summary>
public record GetPublicMenuQuery(Guid RestaurantId) : IRequest<PublicMenuResponse>;

public class GetPublicMenuHandler(SuperFoodDbContext db) : IRequestHandler<GetPublicMenuQuery, PublicMenuResponse>
{
    public async Task<PublicMenuResponse> Handle(GetPublicMenuQuery request, CancellationToken cancellationToken)
    {
        var restaurant = await db.Restaurants.IgnoreQueryFilters()
            .Include(r => r.OperatingHours)
            .FirstOrDefaultAsync(r => r.Id == request.RestaurantId, cancellationToken)
            ?? throw new NotFoundException("Restaurant was not found.");

        var categories = await db.Categories.IgnoreQueryFilters()
            .Include(c => c.Products.Where(p => p.IsActive))
            .ThenInclude(p => p.Variations)
            .Where(c => c.RestaurantId == request.RestaurantId && c.IsVisible)
            .OrderBy(c => c.SortOrder)
            .ToListAsync(cancellationToken);

        return new PublicMenuResponse(ToRestaurantInfo(restaurant), categories.Select(ToCategoryDto).ToList());
    }

    internal static PublicRestaurantInfoDto ToRestaurantInfo(Restaurant restaurant) => new(
        restaurant.Id, restaurant.Name, restaurant.LogoUrl, restaurant.DineInEnabled, restaurant.DeliveryEnabled,
        OrderingWindowGuard.IsOpenNow(restaurant));

    private static PublicCategoryDto ToCategoryDto(Category category) => new(
        category.Id, category.Name,
        category.Products.OrderBy(p => p.SortOrder).Select(p => new PublicProductDto(
            p.Id, p.Name, p.Description, p.Price, p.PhotoUrl, p.IsAvailable,
            p.Variations.Select(v => new PublicVariationDto(v.Id, v.Name, v.PriceAdjustment)).ToList())).ToList());
}

public class GetPublicMenuEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/public/restaurants/{restaurantId:guid}/menu",
            async (Guid restaurantId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetPublicMenuQuery(restaurantId), ct)))
        .WithName("GetPublicMenu")
        .WithTags("Catalog")
        .AllowAnonymous();
    }
}
