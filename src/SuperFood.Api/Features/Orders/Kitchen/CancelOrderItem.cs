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

/// <summary>US-0905: cancel a single order item with a reason (mistakes, unavailable items).</summary>
public record CancelOrderItemCommand(Guid RestaurantId, Guid OrderId, Guid ItemId, string Reason) : IRequest;

public class CancelOrderItemHandler(SuperFoodDbContext db, IOrderNotifier notifier) : IRequestHandler<CancelOrderItemCommand>
{
    public async Task Handle(CancelOrderItemCommand request, CancellationToken cancellationToken)
    {
        var order = await db.Orders.Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.RestaurantId == request.RestaurantId, cancellationToken)
            ?? throw new NotFoundException($"Order '{request.OrderId}' was not found.");

        var item = order.Items.FirstOrDefault(i => i.Id == request.ItemId)
            ?? throw new NotFoundException($"Order item '{request.ItemId}' was not found.");

        item.Status = OrderItemStatus.Cancelled;
        item.CancellationReason = request.Reason;
        await db.SaveChangesAsync(cancellationToken);
        await notifier.OrderItemStatusChangedAsync(request.RestaurantId, order.Id, item.Id, item.Status.ToString(), cancellationToken);
    }
}

public class CancelOrderItemEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/orders/{orderId:guid}/items/{itemId:guid}/cancel",
            async (Guid orderId, Guid itemId, CancelOrderItemRequest request, HttpContext http, ISender sender, CancellationToken ct) =>
            {
                var restaurantId = http.User.GetRestaurantId()
                    ?? throw new UnauthorizedAccessException("No restaurant context.");
                await sender.Send(new CancelOrderItemCommand(restaurantId, orderId, itemId, request.Reason), ct);
                return Results.NoContent();
            })
        .WithName("CancelOrderItem")
        .WithTags("Orders")
        .RequirePermission(Permissions.OrdersManage);
    }
}
