using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperFood.Api.Common;
using SuperFood.Api.Common.Exceptions;
using SuperFood.Contracts.Orders;
using SuperFood.Domain;
using SuperFood.Domain.Entities;
using SuperFood.Infrastructure.Persistence;

namespace SuperFood.Api.Features.Orders.DineIn;

/// <summary>US-0705: split a table's bill by moving selected items into a new order.</summary>
public record SplitOrderCommand(Guid RestaurantId, Guid OrderId, List<Guid> ItemIdsForNewOrder) : IRequest<OrderResponse>;

public class SplitOrderHandler(SuperFoodDbContext db) : IRequestHandler<SplitOrderCommand, OrderResponse>
{
    public async Task<OrderResponse> Handle(SplitOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await db.Orders.Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.RestaurantId == request.RestaurantId, cancellationToken)
            ?? throw new NotFoundException($"Order '{request.OrderId}' was not found.");

        var itemsToMove = order.Items.Where(i => request.ItemIdsForNewOrder.Contains(i.Id)).ToList();
        if (itemsToMove.Count == 0 || itemsToMove.Count == order.Items.Count)
        {
            throw new ConflictException("Select at least one, but not all, items to split into a new bill.");
        }

        var newOrder = new Order
        {
            Id = Guid.NewGuid(),
            RestaurantId = order.RestaurantId,
            Type = order.Type,
            TableSessionId = order.TableSessionId,
            Status = order.Status
        };

        foreach (var item in itemsToMove)
        {
            order.Items.Remove(item);
            item.OrderId = newOrder.Id;
            newOrder.Items.Add(item);
        }

        db.Orders.Add(newOrder);
        await db.SaveChangesAsync(cancellationToken);

        return OrderMapping.ToResponse(newOrder);
    }
}

public class SplitOrderEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/orders/{orderId:guid}/split",
            async (Guid orderId, SplitOrderRequest request, HttpContext http, ISender sender, CancellationToken ct) =>
            {
                var restaurantId = http.User.GetRestaurantId()
                    ?? throw new UnauthorizedAccessException("No restaurant context.");
                var result = await sender.Send(new SplitOrderCommand(restaurantId, orderId, request.ItemIdsForNewOrder), ct);
                return Results.Ok(result);
            })
        .WithName("SplitOrder")
        .WithTags("Orders")
        .RequirePermission(Permissions.OrdersManage);
    }
}
