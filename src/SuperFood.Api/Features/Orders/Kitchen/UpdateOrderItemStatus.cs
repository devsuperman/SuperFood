using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperFood.Api.Common;
using SuperFood.Api.Common.Exceptions;
using SuperFood.Contracts.Orders;
using SuperFood.Domain;
using SuperFood.Domain.Enums;
using SuperFood.Infrastructure.Persistence;
using SuperFood.Infrastructure.RealTime;

namespace SuperFood.Api.Features.Orders.Kitchen;

/// <summary>
/// US-0902: mark an item "in preparation" or "ready". When every item on the
/// order is ready, the order itself flips to Ready too, which is what
/// notifies the waiter/customer (US-0903/US-0806).
/// </summary>
public record UpdateOrderItemStatusCommand(Guid RestaurantId, Guid OrderId, Guid ItemId, OrderItemStatus Status) : IRequest;

public class UpdateOrderItemStatusHandler(SuperFoodDbContext db, IOrderNotifier notifier)
    : IRequestHandler<UpdateOrderItemStatusCommand>
{
    public async Task Handle(UpdateOrderItemStatusCommand request, CancellationToken cancellationToken)
    {
        var order = await db.Orders.Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.RestaurantId == request.RestaurantId, cancellationToken)
            ?? throw new NotFoundException($"Order '{request.OrderId}' was not found.");

        var item = order.Items.FirstOrDefault(i => i.Id == request.ItemId)
            ?? throw new NotFoundException($"Order item '{request.ItemId}' was not found.");

        item.Status = request.Status;
        await db.SaveChangesAsync(cancellationToken);
        await notifier.OrderItemStatusChangedAsync(request.RestaurantId, order.Id, item.Id, item.Status.ToString(), cancellationToken);

        var activeItems = order.Items.Where(i => i.Status != OrderItemStatus.Cancelled).ToList();
        if (activeItems.Count > 0 && activeItems.All(i => i.Status == OrderItemStatus.Ready) &&
            order.Status is OrderStatus.Received or OrderStatus.Accepted or OrderStatus.Preparing)
        {
            order.Status = OrderStatus.Ready;
            await db.SaveChangesAsync(cancellationToken);
            await notifier.OrderStatusChangedAsync(request.RestaurantId, order.Id, order.Status.ToString(), cancellationToken);
        }
    }
}

public class UpdateOrderItemStatusEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/orders/{orderId:guid}/items/{itemId:guid}/status",
            async (Guid orderId, Guid itemId, UpdateOrderItemStatusRequest request, HttpContext http,
                ISender sender, CancellationToken ct) =>
            {
                var restaurantId = http.User.GetRestaurantId()
                    ?? throw new UnauthorizedAccessException("No restaurant context.");
                if (!Enum.TryParse<OrderItemStatus>(request.Status, true, out var status))
                {
                    throw new ConflictException("Unknown item status.");
                }

                await sender.Send(new UpdateOrderItemStatusCommand(restaurantId, orderId, itemId, status), ct);
                return Results.NoContent();
            })
        .WithName("UpdateOrderItemStatus")
        .WithTags("Kitchen")
        .RequirePermission(Permissions.KitchenManage);
    }
}
