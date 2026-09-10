using Microsoft.AspNetCore.SignalR;

namespace SuperFood.Infrastructure.RealTime;

public class SignalROrderNotifier(IHubContext<OrdersHub> hubContext) : IOrderNotifier
{
    public Task OrderCreatedAsync(Guid restaurantId, Guid orderId, CancellationToken cancellationToken = default) =>
        hubContext.Clients.Group(OrdersHub.GroupName(restaurantId.ToString()))
            .SendAsync("OrderCreated", new { orderId }, cancellationToken);

    public Task OrderItemStatusChangedAsync(Guid restaurantId, Guid orderId, Guid orderItemId, string status,
        CancellationToken cancellationToken = default) =>
        hubContext.Clients.Group(OrdersHub.GroupName(restaurantId.ToString()))
            .SendAsync("OrderItemStatusChanged", new { orderId, orderItemId, status }, cancellationToken);

    public Task OrderStatusChangedAsync(Guid restaurantId, Guid orderId, string status,
        CancellationToken cancellationToken = default) =>
        hubContext.Clients.Group(OrdersHub.GroupName(restaurantId.ToString()))
            .SendAsync("OrderStatusChanged", new { orderId, status }, cancellationToken);
}
