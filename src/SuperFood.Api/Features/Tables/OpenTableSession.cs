using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperFood.Api.Common;
using SuperFood.Api.Common.Exceptions;
using SuperFood.Contracts.Tables;
using SuperFood.Domain;
using SuperFood.Domain.Entities;
using SuperFood.Domain.Enums;
using SuperFood.Infrastructure.Persistence;

namespace SuperFood.Api.Features.Tables;

/// <summary>US-0604: open a table (start a session) when guests are seated.</summary>
public record OpenTableSessionCommand(Guid RestaurantId, Guid TableId) : IRequest<OpenTableSessionResponse>;

public class OpenTableSessionHandler(SuperFoodDbContext db) : IRequestHandler<OpenTableSessionCommand, OpenTableSessionResponse>
{
    public async Task<OpenTableSessionResponse> Handle(OpenTableSessionCommand request, CancellationToken cancellationToken)
    {
        var table = await db.Tables
            .FirstOrDefaultAsync(t => t.Id == request.TableId && t.RestaurantId == request.RestaurantId, cancellationToken)
            ?? throw new NotFoundException($"Table '{request.TableId}' was not found.");

        if (table.Status != TableStatus.Free)
        {
            throw new ConflictException($"Table '{table.Identifier}' is already {table.Status}.");
        }

        var session = new TableSession
        {
            Id = Guid.NewGuid(),
            RestaurantId = request.RestaurantId,
            TableId = table.Id
        };

        table.Status = TableStatus.Occupied;
        db.TableSessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);

        return new OpenTableSessionResponse(session.Id, session.OpenedAt);
    }
}

public class OpenTableSessionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/tables/{tableId:guid}/sessions",
            async (Guid tableId, HttpContext http, ISender sender, CancellationToken ct) =>
            {
                var restaurantId = http.User.GetRestaurantId()
                    ?? throw new UnauthorizedAccessException("No restaurant context.");
                var result = await sender.Send(new OpenTableSessionCommand(restaurantId, tableId), ct);
                return Results.Created($"/api/tables/{tableId}/sessions/{result.SessionId}", result);
            })
        .WithName("OpenTableSession")
        .WithTags("Tables")
        .RequirePermission(Permissions.TablesManage);
    }
}
