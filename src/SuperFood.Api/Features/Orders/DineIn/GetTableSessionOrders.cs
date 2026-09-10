using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperFood.Api.Common;
using SuperFood.Api.Common.Exceptions;
using SuperFood.Contracts.Orders;
using SuperFood.Infrastructure.Persistence;

namespace SuperFood.Api.Features.Orders.DineIn;

/// <summary>Backs the waiter's table bill view (all rounds for the current open session).</summary>
public record GetTableSessionOrdersQuery(Guid RestaurantId, Guid TableId) : IRequest<List<OrderResponse>>;

public class GetTableSessionOrdersHandler(SuperFoodDbContext db)
    : IRequestHandler<GetTableSessionOrdersQuery, List<OrderResponse>>
{
    public async Task<List<OrderResponse>> Handle(GetTableSessionOrdersQuery request, CancellationToken cancellationToken)
    {
        var session = await db.TableSessions
            .FirstOrDefaultAsync(s => s.TableId == request.TableId && s.RestaurantId == request.RestaurantId && s.ClosedAt == null,
                cancellationToken)
            ?? throw new NotFoundException("This table has no open session.");

        var orders = await db.Orders.Include(o => o.Items)
            .Where(o => o.TableSessionId == session.Id)
            .OrderBy(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

        return orders.Select(OrderMapping.ToResponse).ToList();
    }
}

public class GetTableSessionOrdersEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/tables/{tableId:guid}/session/orders",
            async (Guid tableId, HttpContext http, ISender sender, CancellationToken ct) =>
            {
                var restaurantId = http.User.GetRestaurantId()
                    ?? throw new UnauthorizedAccessException("No restaurant context.");
                return Results.Ok(await sender.Send(new GetTableSessionOrdersQuery(restaurantId, tableId), ct));
            })
        .WithName("GetTableSessionOrders")
        .WithTags("Orders")
        .RequireAuthorization();
    }
}
