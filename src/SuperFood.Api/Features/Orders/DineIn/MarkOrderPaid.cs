using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperFood.Api.Common;
using SuperFood.Api.Common.Exceptions;
using SuperFood.Contracts.Orders;
using SuperFood.Domain;
using SuperFood.Domain.Enums;
using SuperFood.Infrastructure.Persistence;

namespace SuperFood.Api.Features.Orders.DineIn;

/// <summary>US-0706: mark a dine-in order as paid and register the payment method.</summary>
public record MarkOrderPaidCommand(Guid RestaurantId, Guid OrderId, PaymentMethod PaymentMethod) : IRequest;

public class MarkOrderPaidHandler(SuperFoodDbContext db) : IRequestHandler<MarkOrderPaidCommand>
{
    public async Task Handle(MarkOrderPaidCommand request, CancellationToken cancellationToken)
    {
        var order = await db.Orders
            .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.RestaurantId == request.RestaurantId, cancellationToken)
            ?? throw new NotFoundException($"Order '{request.OrderId}' was not found.");

        order.PaymentStatus = PaymentStatus.Paid;
        order.PaymentMethod = request.PaymentMethod;
        await db.SaveChangesAsync(cancellationToken);
    }
}

public class MarkOrderPaidEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/orders/{orderId:guid}/pay",
            async (Guid orderId, MarkOrderPaidRequest request, HttpContext http, ISender sender, CancellationToken ct) =>
            {
                var restaurantId = http.User.GetRestaurantId()
                    ?? throw new UnauthorizedAccessException("No restaurant context.");
                if (!Enum.TryParse<PaymentMethod>(request.PaymentMethod, true, out var method))
                {
                    throw new ConflictException("Unknown payment method.");
                }

                await sender.Send(new MarkOrderPaidCommand(restaurantId, orderId, method), ct);
                return Results.NoContent();
            })
        .WithName("MarkOrderPaid")
        .WithTags("Orders")
        .RequirePermission(Permissions.OrdersManage);
    }
}
