using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperFood.Api.Common;
using SuperFood.Api.Common.Exceptions;
using SuperFood.Contracts.Orders;
using SuperFood.Domain;
using SuperFood.Domain.Enums;
using SuperFood.Infrastructure.Persistence;
using SuperFood.Infrastructure.RealTime;

namespace SuperFood.Api.Features.Orders.Delivery;

/// <summary>
/// US-0804: progress a delivery order through received -> preparing -> ready
/// -> out for delivery -> delivered/completed, without courier assignment.
/// </summary>
public record UpdateDeliveryStatusCommand(Guid RestaurantId, Guid OrderId, OrderStatus Status) : IRequest;

public class UpdateDeliveryStatusHandler(SuperFoodDbContext db, IOrderNotifier notifier)
    : IRequestHandler<UpdateDeliveryStatusCommand>
{
    private static readonly OrderStatus[] ValidTransitions =
    [
        OrderStatus.Accepted, OrderStatus.Preparing, OrderStatus.Ready,
        OrderStatus.OutForDelivery, OrderStatus.Completed
    ];

    public async Task Handle(UpdateDeliveryStatusCommand request, CancellationToken cancellationToken)
    {
        var order = await db.Orders
            .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.RestaurantId == request.RestaurantId
                && o.Type == OrderType.Delivery, cancellationToken)
            ?? throw new NotFoundException($"Delivery order '{request.OrderId}' was not found.");

        if (!ValidTransitions.Contains(request.Status))
        {
            throw new ConflictException("Invalid delivery status.");
        }

        order.Status = request.Status;
        await db.SaveChangesAsync(cancellationToken);
        await notifier.OrderStatusChangedAsync(request.RestaurantId, order.Id, order.Status.ToString(), cancellationToken);
    }
}

public class UpdateDeliveryStatusEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/orders/delivery/{orderId:guid}/status",
            async (Guid orderId, UpdateOrderStatusRequest request, HttpContext http, ISender sender, CancellationToken ct) =>
            {
                var restaurantId = http.User.GetRestaurantId()
                    ?? throw new UnauthorizedAccessException("No restaurant context.");
                if (!Enum.TryParse<OrderStatus>(request.Status, true, out var status))
                {
                    throw new ConflictException("Unknown order status.");
                }

                await sender.Send(new UpdateDeliveryStatusCommand(restaurantId, orderId, status), ct);
                return Results.NoContent();
            })
        .WithName("UpdateDeliveryStatus")
        .WithTags("Orders")
        .RequirePermission(Permissions.OrdersManage);
    }
}
