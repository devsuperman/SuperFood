using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperFood.Api.Common;
using SuperFood.Api.Common.Exceptions;
using SuperFood.Contracts.Orders;
using SuperFood.Domain;
using SuperFood.Infrastructure.Persistence;

namespace SuperFood.Api.Features.Orders.DineIn;

/// <summary>US-0705: merge several of a table's rounds into a single bill.</summary>
public record MergeOrdersCommand(Guid RestaurantId, List<Guid> OrderIds) : IRequest<OrderResponse>;

public class MergeOrdersHandler(SuperFoodDbContext db) : IRequestHandler<MergeOrdersCommand, OrderResponse>
{
    public async Task<OrderResponse> Handle(MergeOrdersCommand request, CancellationToken cancellationToken)
    {
        if (request.OrderIds.Count < 2)
        {
            throw new ConflictException("Select at least two orders to merge.");
        }

        var orders = await db.Orders.Include(o => o.Items)
            .Where(o => request.OrderIds.Contains(o.Id) && o.RestaurantId == request.RestaurantId)
            .ToListAsync(cancellationToken);

        if (orders.Count != request.OrderIds.Count || orders.Select(o => o.TableSessionId).Distinct().Count() != 1)
        {
            throw new ConflictException("All selected orders must belong to the same open table.");
        }

        var target = orders[0];
        foreach (var other in orders.Skip(1))
        {
            foreach (var item in other.Items.ToList())
            {
                other.Items.Remove(item);
                item.OrderId = target.Id;
                target.Items.Add(item);
            }

            db.Orders.Remove(other);
        }

        await db.SaveChangesAsync(cancellationToken);
        return OrderMapping.ToResponse(target);
    }
}

public class MergeOrdersEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/orders/merge",
            async (MergeOrdersRequest request, HttpContext http, ISender sender, CancellationToken ct) =>
            {
                var restaurantId = http.User.GetRestaurantId()
                    ?? throw new UnauthorizedAccessException("No restaurant context.");
                var result = await sender.Send(new MergeOrdersCommand(restaurantId, request.OrderIds), ct);
                return Results.Ok(result);
            })
        .WithName("MergeOrders")
        .WithTags("Orders")
        .RequirePermission(Permissions.OrdersManage);
    }
}
