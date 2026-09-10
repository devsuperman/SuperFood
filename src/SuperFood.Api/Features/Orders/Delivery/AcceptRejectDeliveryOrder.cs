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

/// <summary>US-0803: accept or reject an incoming delivery order to manage capacity.</summary>
public record AcceptRejectDeliveryOrderCommand(Guid RestaurantId, Guid OrderId, bool Accept, string? RejectionReason) : IRequest;

public class AcceptRejectDeliveryOrderHandler(SuperFoodDbContext db, IOrderNotifier notifier)
    : IRequestHandler<AcceptRejectDeliveryOrderCommand>
{
    public async Task Handle(AcceptRejectDeliveryOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await db.Orders
            .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.RestaurantId == request.RestaurantId
                && o.Type == OrderType.Delivery, cancellationToken)
            ?? throw new NotFoundException($"Delivery order '{request.OrderId}' was not found.");

        if (order.Status != OrderStatus.Received)
        {
            throw new ConflictException("This order has already been actioned.");
        }

        order.Status = request.Accept ? OrderStatus.Accepted : OrderStatus.Rejected;
        if (!request.Accept)
        {
            order.CancellationReason = request.RejectionReason;
        }

        await db.SaveChangesAsync(cancellationToken);
        await notifier.OrderStatusChangedAsync(request.RestaurantId, order.Id, order.Status.ToString(), cancellationToken);
    }
}

public class AcceptRejectDeliveryOrderEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/orders/delivery/{orderId:guid}/accept",
            async (Guid orderId, HttpContext http, ISender sender, CancellationToken ct) =>
            {
                var restaurantId = http.User.GetRestaurantId()
                    ?? throw new UnauthorizedAccessException("No restaurant context.");
                await sender.Send(new AcceptRejectDeliveryOrderCommand(restaurantId, orderId, true, null), ct);
                return Results.NoContent();
            })
        .WithName("AcceptDeliveryOrder")
        .WithTags("Orders")
        .RequirePermission(Permissions.OrdersManage);

        app.MapPost("/api/orders/delivery/{orderId:guid}/reject",
            async (Guid orderId, CancelOrderRequest request, HttpContext http, ISender sender, CancellationToken ct) =>
            {
                var restaurantId = http.User.GetRestaurantId()
                    ?? throw new UnauthorizedAccessException("No restaurant context.");
                await sender.Send(new AcceptRejectDeliveryOrderCommand(restaurantId, orderId, false, request.Reason), ct);
                return Results.NoContent();
            })
        .WithName("RejectDeliveryOrder")
        .WithTags("Orders")
        .RequirePermission(Permissions.OrdersManage);
    }
}
