using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperFood.Api.Common;
using SuperFood.Contracts.Orders;
using SuperFood.Domain;
using SuperFood.Domain.Enums;
using SuperFood.Infrastructure.Persistence;

namespace SuperFood.Api.Features.Orders.Kitchen;

/// <summary>US-0904: history of completed/cancelled orders for operational review.</summary>
public record GetOrderHistoryQuery(Guid RestaurantId) : IRequest<List<OrderResponse>>;

public class GetOrderHistoryHandler(SuperFoodDbContext db) : IRequestHandler<GetOrderHistoryQuery, List<OrderResponse>>
{
    public async Task<List<OrderResponse>> Handle(GetOrderHistoryQuery request, CancellationToken cancellationToken)
    {
        var orders = await db.Orders.Include(o => o.Items)
            .Where(o => o.RestaurantId == request.RestaurantId &&
                (o.Status == OrderStatus.Completed || o.Status == OrderStatus.Cancelled || o.Status == OrderStatus.Rejected))
            .OrderByDescending(o => o.CreatedAt)
            .Take(200)
            .ToListAsync(cancellationToken);

        return orders.Select(OrderMapping.ToResponse).ToList();
    }
}

public class GetOrderHistoryEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/orders/history", async (HttpContext http, ISender sender, CancellationToken ct) =>
        {
            var restaurantId = http.User.GetRestaurantId()
                ?? throw new UnauthorizedAccessException("No restaurant context.");
            return Results.Ok(await sender.Send(new GetOrderHistoryQuery(restaurantId), ct));
        })
        .WithName("GetOrderHistory")
        .WithTags("Orders")
        .RequirePermission(Permissions.OrdersManage);
    }
}
