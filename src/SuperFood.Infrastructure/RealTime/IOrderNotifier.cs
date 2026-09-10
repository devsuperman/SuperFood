namespace SuperFood.Infrastructure.RealTime;

/// <summary>
/// Published by slice handlers after a command commits successfully — never
/// speculatively before persistence (docs/tech-stack.md §7).
/// </summary>
public interface IOrderNotifier
{
    Task OrderCreatedAsync(Guid restaurantId, Guid orderId, CancellationToken cancellationToken = default);

    Task OrderItemStatusChangedAsync(Guid restaurantId, Guid orderId, Guid orderItemId, string status,
        CancellationToken cancellationToken = default);

    Task OrderStatusChangedAsync(Guid restaurantId, Guid orderId, string status,
        CancellationToken cancellationToken = default);
}
