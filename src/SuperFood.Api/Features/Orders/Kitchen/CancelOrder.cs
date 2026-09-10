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

/// <summary>US-0905: cancel an entire order with a reason.</summary>
public record CancelOrderCommand(Guid RestaurantId, Guid OrderId, string Reason) : IRequest;

public class CancelOrderHandler(SuperFoodDbContext db, IOrderNotifier notifier) : IRequestHandler<CancelOrderCommand>
{
    public async Task Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await db.Orders
            .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.RestaurantId == request.RestaurantId, cancellationToken)
            ?? throw new NotFoundException($"Order '{request.OrderId}' was not found.");

        if (order.Status is OrderStatus.Completed or OrderStatus.Cancelled)
        {
            throw new ConflictException("This order can no longer be cancelled.");
        }

        order.Status = OrderStatus.Cancelled;
        order.CancellationReason = request.Reason;
        await db.SaveChangesAsync(cancellationToken);
        await notifier.OrderStatusChangedAsync(request.RestaurantId, order.Id, order.Status.ToString(), cancellationToken);
    }
}

public class CancelOrderEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/orders/{orderId:guid}/cancel",
            async (Guid orderId, CancelOrderRequest request, HttpContext http, ISender sender, CancellationToken ct) =>
            {
                var restaurantId = http.User.GetRestaurantId()
                    ?? throw new UnauthorizedAccessException("No restaurant context.");
                await sender.Send(new CancelOrderCommand(restaurantId, orderId, request.Reason), ct);
                return Results.NoContent();
            })
        .WithName("CancelOrder")
        .WithTags("Orders")
        .RequirePermission(Permissions.OrdersManage);
    }
}
