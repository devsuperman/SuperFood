using SuperFood.Domain.Enums;

namespace SuperFood.Domain.Entities;

/// <summary>EPIC-07 (dine-in) / EPIC-08 (delivery) / EPIC-09 (lifecycle).</summary>
public class Order
{
    public Guid Id { get; set; }
    public Guid RestaurantId { get; set; }
    public OrderType Type { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Received;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;
    public PaymentMethod? PaymentMethod { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? CancellationReason { get; set; }

    /// <summary>Set for dine-in orders (US-0604/US-0703).</summary>
    public Guid? TableSessionId { get; set; }
    public TableSession? TableSession { get; set; }

    /// <summary>Set for delivery orders (US-0801).</summary>
    public DeliveryDetails? DeliveryDetails { get; set; }

    /// <summary>
    /// Anonymous browser-generated id correlating a delivery customer to their
    /// order for status lookup (US-0806) without requiring a customer account.
    /// </summary>
    public string? CustomerSessionId { get; set; }

    public List<OrderItem> Items { get; set; } = [];

    public decimal Total => Items.Where(i => i.Status != OrderItemStatus.Cancelled)
        .Sum(i => (i.UnitPrice + i.VariationPriceAdjustment) * i.Quantity);
}
