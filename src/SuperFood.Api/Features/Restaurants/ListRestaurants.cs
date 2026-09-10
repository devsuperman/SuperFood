using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperFood.Api.Common;
using SuperFood.Contracts.Restaurants;
using SuperFood.Infrastructure.Persistence;

namespace SuperFood.Api.Features.Restaurants;

/// <summary>US-0102: platform_admin lists all restaurants, filterable by status.</summary>
public record ListRestaurantsQuery(string? Status, string? Search) : IRequest<List<RestaurantSummaryResponse>>;

public class ListRestaurantsHandler(SuperFoodDbContext db)
    : IRequestHandler<ListRestaurantsQuery, List<RestaurantSummaryResponse>>
{
    public async Task<List<RestaurantSummaryResponse>> Handle(ListRestaurantsQuery request, CancellationToken cancellationToken)
    {
        var query = db.Restaurants.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<Domain.Enums.RestaurantStatus>(request.Status, true, out var status))
        {
            query = query.Where(r => r.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(r => r.Name.Contains(request.Search));
        }

        return await query
            .OrderBy(r => r.Name)
            .Select(r => new RestaurantSummaryResponse(r.Id, r.Name, r.Status.ToString(), r.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}

public class ListRestaurantsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/restaurants", async (string? status, string? search, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new ListRestaurantsQuery(status, search), ct)))
        .WithName("ListRestaurants")
        .WithTags("Restaurants")
        .RequirePlatformAdmin();
    }
}
