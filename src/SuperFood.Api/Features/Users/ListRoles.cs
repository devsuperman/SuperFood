using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperFood.Api.Common;
using SuperFood.Contracts.Users;
using SuperFood.Domain.Entities;
using SuperFood.Infrastructure.Persistence;

namespace SuperFood.Api.Features.Users;

public record ListRolesQuery(Guid RestaurantId) : IRequest<List<RoleResponse>>;

public class ListRolesHandler(SuperFoodDbContext db) : IRequestHandler<ListRolesQuery, List<RoleResponse>>
{
    public Task<List<RoleResponse>> Handle(ListRolesQuery request, CancellationToken cancellationToken) =>
        db.Set<Role>()
            .Where(r => r.RestaurantId == request.RestaurantId)
            .OrderBy(r => r.Name)
            .Select(r => new RoleResponse(r.Id, r.Name, r.Permissions))
            .ToListAsync(cancellationToken);
}

public class ListRolesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users/roles", async (HttpContext http, ISender sender, CancellationToken ct) =>
        {
            var restaurantId = http.User.GetRestaurantId()
                ?? throw new UnauthorizedAccessException("No restaurant context.");
            return Results.Ok(await sender.Send(new ListRolesQuery(restaurantId), ct));
        })
        .WithName("ListRoles")
        .WithTags("Users")
        .RequireAuthorization();
    }
}
