using Microsoft.EntityFrameworkCore;
using SuperFood.Api.Common.Exceptions;
using SuperFood.Contracts.Orders;
using SuperFood.Domain.Entities;
using SuperFood.Domain.Enums;
using SuperFood.Infrastructure.Persistence;

namespace SuperFood.Api.Features.Orders;

/// <summary>
/// Builds order-line snapshots from live product/variation data (US-0702),
/// rejecting unavailable/inactive products so a stale customer cart can't
/// order something no longer on the menu.
/// </summary>
internal static class OrderItemFactory
{
    public static async Task<List<OrderItem>> BuildAsync(
        SuperFoodDbContext db, Guid restaurantId, List<OrderItemInputDto> items, CancellationToken cancellationToken)
    {
        if (items.Count == 0)
        {
            throw new ConflictException("An order must contain at least one item.");
        }

        // IgnoreQueryFilters + an explicit restaurantId match: this factory is
        // also used from anonymous customer endpoints (US-0701/US-0801) where
        // no tenant claim exists to satisfy the global query filter.
        var productIds = items.Select(i => i.ProductId).Distinct().ToList();
        var products = await db.Products.IgnoreQueryFilters().Include(p => p.Variations)
            .Where(p => p.RestaurantId == restaurantId && productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var orderItems = new List<OrderItem>();
        foreach (var input in items)
        {
            if (!products.TryGetValue(input.ProductId, out var product) || !product.IsActive || !product.IsAvailable)
            {
                throw new ConflictException($"Product '{input.ProductId}' is not available to order right now.");
            }

            ProductVariation? variation = null;
            if (input.ProductVariationId is { } variationId)
            {
                variation = product.Variations.FirstOrDefault(v => v.Id == variationId)
                    ?? throw new NotFoundException($"Variation '{variationId}' was not found for this product.");
            }

            orderItems.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                RestaurantId = restaurantId,
                ProductId = product.Id,
                ProductVariationId = variation?.Id,
                ProductName = product.Name,
                VariationName = variation?.Name,
                UnitPrice = product.Price,
                VariationPriceAdjustment = variation?.PriceAdjustment ?? 0,
                Quantity = input.Quantity <= 0 ? 1 : input.Quantity,
                Notes = input.Notes,
                Status = OrderItemStatus.Pending
            });
        }

        return orderItems;
    }
}
