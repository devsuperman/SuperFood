using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperFood.Api.Common;
using SuperFood.Api.Common.Exceptions;
using SuperFood.Contracts.Orders;
using SuperFood.Domain.Enums;
using SuperFood.Infrastructure.Persistence;

namespace SuperFood.Api.Features.Orders.Delivery;

/// <summary>
/// US-0806: an anonymous customer checks their delivery order's status,
/// correlated by the anonymous session id captured at order time rather than
/// a customer account (docs/tech-stack.md §5).
/// </summary>
public record GetDeliveryOrderStatusQuery(Guid OrderId, string CustomerSessionId) : IRequest<OrderResponse>;

public class GetDeliveryOrderStatusHandler(SuperFoodDbContext db) : IRequestHandler<GetDeliveryOrderStatusQuery, OrderResponse>
{
    public async Task<OrderResponse> Handle(GetDeliveryOrderStatusQuery request, CancellationToken cancellationToken)
    {
        var order = await db.Orders.IgnoreQueryFilters().Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.Type == OrderType.Delivery
                && o.CustomerSessionId == request.CustomerSessionId, cancellationToken)
            ?? throw new NotFoundException("Order was not found.");

        return OrderMapping.ToResponse(order);
    }
}

public class GetDeliveryOrderStatusEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/public/orders/{orderId:guid}/status",
            async (Guid orderId, string customerSessionId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetDeliveryOrderStatusQuery(orderId, customerSessionId), ct)))
        .WithName("GetDeliveryOrderStatus")
        .WithTags("Orders")
        .AllowAnonymous();
    }
}
