using SuperFood.Contracts.Orders;
using SuperFood.Domain.Entities;

namespace SuperFood.Api.Features.Orders;

/// <summary>Shared response projection for the Order aggregate, used by DineIn/Delivery/Kitchen slices.</summary>
internal static class OrderMapping
{
    public static OrderResponse ToResponse(Order order) => new(
        order.Id,
        order.Type.ToString(),
        order.Status.ToString(),
        order.PaymentStatus.ToString(),
        order.PaymentMethod?.ToString(),
        order.CreatedAt,
        order.TableSessionId,
        order.DeliveryDetails is null
            ? null
            : new DeliveryDetailsDto(order.DeliveryDetails.CustomerName, order.DeliveryDetails.Address, order.DeliveryDetails.ContactPhone),
        order.Items.Select(i => new OrderItemResponse(
            i.Id, i.ProductId, i.ProductName, i.VariationName, i.UnitPrice, i.VariationPriceAdjustment,
            i.Quantity, i.Notes, i.Status.ToString(), i.CancellationReason)).ToList(),
        order.Total);
}
