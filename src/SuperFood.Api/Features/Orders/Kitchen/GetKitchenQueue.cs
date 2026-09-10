using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperFood.Api.Common;
using SuperFood.Contracts.Orders;
using SuperFood.Domain;
using SuperFood.Domain.Enums;
using SuperFood.Infrastructure.Persistence;

namespace SuperFood.Api.Features.Orders.Kitchen;

/// <summary>US-0901: live queue of pending order items across dine-in and delivery.</summary>
public record GetKitchenQueueQuery(Guid RestaurantId) : IRequest<List<OrderResponse>>;

public class GetKitchenQueueHandler(SuperFoodDbContext db) : IRequestHandler<GetKitchenQueueQuery, List<OrderResponse>>
{
    public async Task<List<OrderResponse>> Handle(GetKitchenQueueQuery request, CancellationToken cancellationToken)
    {
        var orders = await db.Orders.Include(o => o.Items)
            .Where(o => o.RestaurantId == request.RestaurantId
                && o.Status != OrderStatus.Completed && o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Rejected
                && o.Items.Any(i => i.Status == OrderItemStatus.Pending || i.Status == OrderItemStatus.Preparing))
            .OrderBy(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

        return orders.Select(OrderMapping.ToResponse).ToList();
    }
}

public class GetKitchenQueueEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/kitchen/queue", async (HttpContext http, ISender sender, CancellationToken ct) =>
        {
            var restaurantId = http.User.GetRestaurantId()
                ?? throw new UnauthorizedAccessException("No restaurant context.");
            return Results.Ok(await sender.Send(new GetKitchenQueueQuery(restaurantId), ct));
        })
        .WithName("GetKitchenQueue")
        .WithTags("Kitchen")
        .RequirePermission(Permissions.KitchenManage);
    }
}
