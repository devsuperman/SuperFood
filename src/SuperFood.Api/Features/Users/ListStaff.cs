using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperFood.Api.Common;
using SuperFood.Contracts.Users;
using SuperFood.Domain;
using SuperFood.Domain.Entities;
using SuperFood.Infrastructure.Identity;
using SuperFood.Infrastructure.Persistence;

namespace SuperFood.Api.Features.Users;

/// <summary>Backs the staff/role management screens (EPIC-03).</summary>
public record ListStaffQuery(Guid RestaurantId) : IRequest<List<StaffSummaryResponse>>;

public class ListStaffHandler(SuperFoodDbContext db) : IRequestHandler<ListStaffQuery, List<StaffSummaryResponse>>
{
    public async Task<List<StaffSummaryResponse>> Handle(ListStaffQuery request, CancellationToken cancellationToken)
    {
        var roles = await db.Set<Role>().ToDictionaryAsync(r => r.Id, r => r.Name, cancellationToken);

        var staff = await db.Set<ApplicationUser>()
            .Where(u => u.RestaurantId == request.RestaurantId)
            .OrderBy(u => u.FullName)
            .Select(u => new { u.Id, u.FullName, u.Email, u.RoleId, u.IsActive })
            .ToListAsync(cancellationToken);

        return staff.Select(u => new StaffSummaryResponse(
            u.Id, u.FullName, u.Email!, u.RoleId,
            u.RoleId.HasValue && roles.TryGetValue(u.RoleId.Value, out var name) ? name : null,
            u.IsActive)).ToList();
    }
}

public class ListStaffEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users/staff", async (HttpContext http, ISender sender, CancellationToken ct) =>
        {
            var restaurantId = http.User.GetRestaurantId()
                ?? throw new UnauthorizedAccessException("No restaurant context.");
            return Results.Ok(await sender.Send(new ListStaffQuery(restaurantId), ct));
        })
        .WithName("ListStaff")
        .WithTags("Users")
        .RequirePermission(Permissions.UsersManage);
    }
}
