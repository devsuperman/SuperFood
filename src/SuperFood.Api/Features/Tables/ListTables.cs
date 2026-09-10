using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SuperFood.Api.Common;
using SuperFood.Contracts.Tables;
using SuperFood.Infrastructure.Persistence;

namespace SuperFood.Api.Features.Tables;

/// <summary>US-0603: current status of every table.</summary>
public record ListTablesQuery(Guid RestaurantId) : IRequest<List<TableResponse>>;

public class ListTablesHandler(SuperFoodDbContext db, IConfiguration configuration)
    : IRequestHandler<ListTablesQuery, List<TableResponse>>
{
    public async Task<List<TableResponse>> Handle(ListTablesQuery request, CancellationToken cancellationToken)
    {
        var tables = await db.Tables
            .Where(t => t.RestaurantId == request.RestaurantId)
            .OrderBy(t => t.Identifier)
            .ToListAsync(cancellationToken);

        return tables.Select(t => TableEndpointHelpers.ToResponse(t, request.RestaurantId, configuration)).ToList();
    }
}

public class ListTablesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/tables", async (HttpContext http, ISender sender, CancellationToken ct) =>
        {
            var restaurantId = http.User.GetRestaurantId()
                ?? throw new UnauthorizedAccessException("No restaurant context.");
            return Results.Ok(await sender.Send(new ListTablesQuery(restaurantId), ct));
        })
        .WithName("ListTables")
        .WithTags("Tables")
        .RequireAuthorization();
    }
}
