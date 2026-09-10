using FluentValidation;
using MediatR;
using SuperFood.Api.Common;
using SuperFood.Contracts.Orders;
using SuperFood.Domain.Entities;
using SuperFood.Domain.Enums;
using SuperFood.Infrastructure.Persistence;
using SuperFood.Infrastructure.RealTime;

namespace SuperFood.Api.Features.Orders.Delivery;

/// <summary>US-0801: customer places a delivery order with their address and contact info.</summary>
public record CreateDeliveryOrderCommand(
    Guid RestaurantId, DeliveryDetailsDto DeliveryDetails, List<OrderItemInputDto> Items, string CustomerSessionId)
    : IRequest<OrderResponse>;

public class CreateDeliveryOrderValidator : AbstractValidator<CreateDeliveryOrderCommand>
{
    public CreateDeliveryOrderValidator()
    {
        RuleFor(x => x.Items).NotEmpty();
        RuleFor(x => x.DeliveryDetails.CustomerName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.DeliveryDetails.Address).NotEmpty().MaximumLength(300);
        RuleFor(x => x.DeliveryDetails.ContactPhone).NotEmpty().MaximumLength(30);
        RuleFor(x => x.CustomerSessionId).NotEmpty();
    }
}

public class CreateDeliveryOrderHandler(SuperFoodDbContext db, IOrderNotifier notifier)
    : IRequestHandler<CreateDeliveryOrderCommand, OrderResponse>
{
    public async Task<OrderResponse> Handle(CreateDeliveryOrderCommand request, CancellationToken cancellationToken)
    {
        await OrderingWindowGuard.EnsureCanOrderAsync(db, request.RestaurantId, dineIn: false, cancellationToken);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            RestaurantId = request.RestaurantId,
            Type = OrderType.Delivery,
            CustomerSessionId = request.CustomerSessionId,
            DeliveryDetails = new DeliveryDetails
            {
                CustomerName = request.DeliveryDetails.CustomerName,
                Address = request.DeliveryDetails.Address,
                ContactPhone = request.DeliveryDetails.ContactPhone
            },
            Items = await OrderItemFactory.BuildAsync(db, request.RestaurantId, request.Items, cancellationToken)
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);
        await notifier.OrderCreatedAsync(request.RestaurantId, order.Id, cancellationToken);

        return OrderMapping.ToResponse(order);
    }
}

public class CreateDeliveryOrderEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/public/restaurants/{restaurantId:guid}/delivery-orders",
            async (Guid restaurantId, CreateDeliveryOrderRequest request, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new CreateDeliveryOrderCommand(
                    restaurantId, request.DeliveryDetails, request.Items, request.CustomerSessionId), ct);
                return Results.Created($"/api/public/orders/{result.Id}", result);
            })
        .WithName("CreateDeliveryOrder")
        .WithTags("Orders")
        .AllowAnonymous();
    }
}
