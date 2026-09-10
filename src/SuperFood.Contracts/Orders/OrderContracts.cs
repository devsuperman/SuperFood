namespace SuperFood.Contracts.Orders;

public record OrderItemInputDto(Guid ProductId, Guid? ProductVariationId, int Quantity, string? Notes);

public record OrderItemResponse(
    Guid Id, Guid ProductId, string ProductName, string? VariationName, decimal UnitPrice,
    decimal VariationPriceAdjustment, int Quantity, string? Notes, string Status, string? CancellationReason);

public record DeliveryDetailsDto(string CustomerName, string Address, string ContactPhone);

public record OrderResponse(
    Guid Id,
    string Type,
    string Status,
    string PaymentStatus,
    string? PaymentMethod,
    DateTimeOffset CreatedAt,
    Guid? TableSessionId,
    DeliveryDetailsDto? DeliveryDetails,
    List<OrderItemResponse> Items,
    decimal Total);

public record CreateDineInOrderRequest(Guid TableId, List<OrderItemInputDto> Items);

public record CreateDeliveryOrderRequest(DeliveryDetailsDto DeliveryDetails, List<OrderItemInputDto> Items, string CustomerSessionId);

public record MarkOrderPaidRequest(string PaymentMethod);

public record UpdateOrderStatusRequest(string Status);

public record UpdateOrderItemStatusRequest(string Status);

public record CancelOrderItemRequest(string Reason);

public record CancelOrderRequest(string Reason);

public record SplitOrderRequest(List<Guid> ItemIdsForNewOrder);

public record MergeOrdersRequest(List<Guid> OrderIds);
