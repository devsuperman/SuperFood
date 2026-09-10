using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperFood.Api.Common;
using SuperFood.Api.Common.Exceptions;
using SuperFood.Contracts.Orders;
using SuperFood.Domain;
using SuperFood.Domain.Enums;
using SuperFood.Infrastructure.Persistence;

namespace SuperFood.Api.Features.Orders.Delivery;

/// <summary>US-0805: record a delivery order's payment status/method (paid online / cash on delivery).</summary>
public record MarkDeliveryPaymentStatusCommand(Guid RestaurantId, Guid OrderId, PaymentMethod PaymentMethod) : IRequest;

public class MarkDeliveryPaymentStatusHandler(SuperFoodDbContext db) : IRequestHandler<MarkDeliveryPaymentStatusCommand>
{
    public async Task Handle(MarkDeliveryPaymentStatusCommand request, CancellationToken cancellationToken)
    {
        var order = await db.Orders
            .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.RestaurantId == request.RestaurantId
                && o.Type == OrderType.Delivery, cancellationToken)
            ?? throw new NotFoundException($"Delivery order '{request.OrderId}' was not found.");

        order.PaymentMethod = request.PaymentMethod;
        order.PaymentStatus = request.PaymentMethod == PaymentMethod.CashOnDelivery
            ? PaymentStatus.Unpaid
            : PaymentStatus.Paid;

        await db.SaveChangesAsync(cancellationToken);
    }
}

public class MarkDeliveryPaymentStatusEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/orders/delivery/{orderId:guid}/payment",
            async (Guid orderId, MarkOrderPaidRequest request, HttpContext http, ISender sender, CancellationToken ct) =>
            {
                var restaurantId = http.User.GetRestaurantId()
                    ?? throw new UnauthorizedAccessException("No restaurant context.");
                if (!Enum.TryParse<PaymentMethod>(request.PaymentMethod, true, out var method))
                {
                    throw new ConflictException("Unknown payment method.");
                }

                await sender.Send(new MarkDeliveryPaymentStatusCommand(restaurantId, orderId, method), ct);
                return Results.NoContent();
            })
        .WithName("MarkDeliveryPaymentStatus")
        .WithTags("Orders")
        .RequirePermission(Permissions.OrdersManage);
    }
}
