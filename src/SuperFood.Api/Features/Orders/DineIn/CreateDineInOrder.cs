using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperFood.Api.Common;
using SuperFood.Api.Common.Exceptions;
using SuperFood.Contracts.Orders;
using SuperFood.Domain;
using SuperFood.Domain.Entities;
using SuperFood.Domain.Enums;
using SuperFood.Infrastructure.Persistence;
using SuperFood.Infrastructure.RealTime;

namespace SuperFood.Api.Features.Orders.DineIn;

/// <summary>
/// US-0701/US-0702 (customer, via table QR) and US-0703/US-0704 (waiter,
/// on behalf of a table). Each call creates one order "round" attached to
/// the table's open session — a table naturally accumulates multiple rounds
/// this way rather than one order being mutated in place.
/// </summary>
public record CreateDineInOrderCommand(Guid RestaurantId, Guid TableId, List<OrderItemInputDto> Items)
    : IRequest<OrderResponse>;

public class CreateDineInOrderValidator : AbstractValidator<CreateDineInOrderCommand>
{
    public CreateDineInOrderValidator() => RuleFor(x => x.Items).NotEmpty();
}

public class CreateDineInOrderHandler(SuperFoodDbContext db, IOrderNotifier notifier)
    : IRequestHandler<CreateDineInOrderCommand, OrderResponse>
{
    public async Task<OrderResponse> Handle(CreateDineInOrderCommand request, CancellationToken cancellationToken)
    {
        await OrderingWindowGuard.EnsureCanOrderAsync(db, request.RestaurantId, dineIn: true, cancellationToken);

        var table = await db.Tables.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == request.TableId && t.RestaurantId == request.RestaurantId, cancellationToken)
            ?? throw new NotFoundException("Table was not found.");

        // Auto-open the table for the first round (US-0701: order without waiting for a waiter).
        var session = await db.TableSessions.IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.TableId == table.Id && s.ClosedAt == null, cancellationToken);

        if (session is null)
        {
            session = new TableSession { Id = Guid.NewGuid(), RestaurantId = request.RestaurantId, TableId = table.Id };
            db.TableSessions.Add(session);
            table.Status = TableStatus.Occupied;
        }

        var order = new Order
        {
            Id = Guid.NewGuid(),
            RestaurantId = request.RestaurantId,
            Type = OrderType.DineIn,
            TableSessionId = session.Id,
            Items = await OrderItemFactory.BuildAsync(db, request.RestaurantId, request.Items, cancellationToken)
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);
        await notifier.OrderCreatedAsync(request.RestaurantId, order.Id, cancellationToken);

        return OrderMapping.ToResponse(order);
    }
}

public class CreateDineInOrderEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        // US-0701/US-0702: customer, anonymous, scanned from the table's QR code.
        app.MapPost("/api/public/restaurants/{restaurantId:guid}/tables/{qrToken}/orders",
            async (Guid restaurantId, string qrToken, List<OrderItemInputDto> items, SuperFoodDbContext db,
                ISender sender, CancellationToken ct) =>
            {
                var table = await db.Tables.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(t => t.RestaurantId == restaurantId && t.QrCodeToken == qrToken, ct)
                    ?? throw new NotFoundException("Table was not found.");

                var result = await sender.Send(new CreateDineInOrderCommand(restaurantId, table.Id, items), ct);
                return Results.Created($"/api/public/orders/{result.Id}", result);
            })
        .WithName("CreateDineInOrderAsCustomer")
        .WithTags("Orders")
        .AllowAnonymous();

        // US-0703/US-0704: waiter creates/adds a round on behalf of a table.
        app.MapPost("/api/tables/{tableId:guid}/orders",
            async (Guid tableId, List<OrderItemInputDto> items, HttpContext http, ISender sender, CancellationToken ct) =>
            {
                var restaurantId = http.User.GetRestaurantId()
                    ?? throw new UnauthorizedAccessException("No restaurant context.");
                var result = await sender.Send(new CreateDineInOrderCommand(restaurantId, tableId, items), ct);
                return Results.Created($"/api/orders/{result.Id}", result);
            })
        .WithName("CreateDineInOrderAsWaiter")
        .WithTags("Orders")
        .RequirePermission(Permissions.OrdersManage);
    }
}
