using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperFood.Api.Common;
using SuperFood.Contracts.Orders;
using SuperFood.Domain.Enums;
using SuperFood.Infrastructure.Persistence;

namespace SuperFood.Api.Features.Orders.Delivery;

/// <summary>US-0802: incoming delivery orders queue for staff.</summary>
public record ListDeliveryQueueQuery(Guid RestaurantId) : IRequest<List<OrderResponse>>;

public class ListDeliveryQueueHandler(SuperFoodDbContext db) : IRequestHandler<ListDeliveryQueueQuery, List<OrderResponse>>
{
    public async Task<List<OrderResponse>> Handle(ListDeliveryQueueQuery request, CancellationToken cancellationToken)
    {
        var orders = await db.Orders.Include(o => o.Items)
            .Where(o => o.RestaurantId == request.RestaurantId && o.Type == OrderType.Delivery
                && o.Status != OrderStatus.Completed && o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Rejected)
            .OrderBy(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

        return orders.Select(OrderMapping.ToResponse).ToList();
    }
}

public class ListDeliveryQueueEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/orders/delivery/queue", async (HttpContext http, ISender sender, CancellationToken ct) =>
        {
            var restaurantId = http.User.GetRestaurantId()
                ?? throw new UnauthorizedAccessException("No restaurant context.");
            return Results.Ok(await sender.Send(new ListDeliveryQueueQuery(restaurantId), ct));
        })
        .WithName("ListDeliveryQueue")
        .WithTags("Orders")
        .RequirePermission(Domain.Permissions.OrdersManage);
    }
}
