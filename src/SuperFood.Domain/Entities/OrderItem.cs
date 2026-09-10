using SuperFood.Domain.Enums;

namespace SuperFood.Domain.Entities;

/// <summary>A line item on an <see cref="Order"/> (US-0702/US-0902).</summary>
public class OrderItem
{
    public Guid Id { get; set; }
    public Guid RestaurantId { get; set; }
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? ProductVariationId { get; set; }

    // Snapshotted at order time so later menu edits don't rewrite history.
    public string ProductName { get; set; } = string.Empty;
    public string? VariationName { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal VariationPriceAdjustment { get; set; }

    public int Quantity { get; set; } = 1;
    public string? Notes { get; set; }
    public OrderItemStatus Status { get; set; } = OrderItemStatus.Pending;
    public string? CancellationReason { get; set; }
}
