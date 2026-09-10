using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperFood.Api.Common;
using SuperFood.Api.Common.Exceptions;
using SuperFood.Domain;
using SuperFood.Domain.Enums;
using SuperFood.Infrastructure.Persistence;

namespace SuperFood.Api.Features.Tables;

/// <summary>US-0605: close a table (end session) after payment, freeing it back up.</summary>
public record CloseTableSessionCommand(Guid RestaurantId, Guid TableId, Guid SessionId) : IRequest;

public class CloseTableSessionHandler(SuperFoodDbContext db) : IRequestHandler<CloseTableSessionCommand>
{
    public async Task Handle(CloseTableSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await db.TableSessions
            .Include(s => s.Orders)
            .FirstOrDefaultAsync(s => s.Id == request.SessionId && s.TableId == request.TableId
                && s.RestaurantId == request.RestaurantId, cancellationToken)
            ?? throw new NotFoundException($"Table session '{request.SessionId}' was not found.");

        if (!session.IsOpen)
        {
            throw new ConflictException("This table session is already closed.");
        }

        var unpaidOrders = session.Orders.Any(o =>
            o.Status != OrderStatus.Cancelled && o.PaymentStatus != PaymentStatus.Paid);
        if (unpaidOrders)
        {
            throw new ConflictException("All orders for this table must be paid before closing it.");
        }

        session.ClosedAt = DateTimeOffset.UtcNow;

        var table = await db.Tables.FirstAsync(t => t.Id == request.TableId, cancellationToken);
        table.Status = TableStatus.Free;

        await db.SaveChangesAsync(cancellationToken);
    }
}

public class CloseTableSessionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/tables/{tableId:guid}/sessions/{sessionId:guid}/close",
            async (Guid tableId, Guid sessionId, HttpContext http, ISender sender, CancellationToken ct) =>
            {
                var restaurantId = http.User.GetRestaurantId()
                    ?? throw new UnauthorizedAccessException("No restaurant context.");
                await sender.Send(new CloseTableSessionCommand(restaurantId, tableId, sessionId), ct);
                return Results.NoContent();
            })
        .WithName("CloseTableSession")
        .WithTags("Tables")
        .RequirePermission(Permissions.TablesManage);
    }
}
