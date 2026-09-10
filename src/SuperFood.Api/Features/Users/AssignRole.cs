using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperFood.Api.Common;
using SuperFood.Api.Common.Exceptions;
using SuperFood.Contracts.Users;
using SuperFood.Domain;
using SuperFood.Domain.Entities;
using SuperFood.Infrastructure.Identity;
using SuperFood.Infrastructure.Persistence;

namespace SuperFood.Api.Features.Users;

/// <summary>
/// US-0303: assign a role to a staff member. Simplified to a single role per
/// user (a Role already aggregates a permission set, see ADR-06) rather than
/// a many-to-many, which covers the story's intent without extra join-table
/// machinery.
/// </summary>
public record AssignRoleCommand(Guid RestaurantId, Guid UserId, Guid RoleId) : IRequest;

public class AssignRoleHandler(SuperFoodDbContext db) : IRequestHandler<AssignRoleCommand>
{
    public async Task Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await db.Set<ApplicationUser>()
            .FirstOrDefaultAsync(u => u.Id == request.UserId && u.RestaurantId == request.RestaurantId, cancellationToken)
            ?? throw new NotFoundException($"Staff member '{request.UserId}' was not found.");

        var roleExists = await db.Set<Role>().AnyAsync(r => r.Id == request.RoleId, cancellationToken);
        if (!roleExists)
        {
            throw new NotFoundException($"Role '{request.RoleId}' was not found.");
        }

        user.RoleId = request.RoleId;
        await db.SaveChangesAsync(cancellationToken);
    }
}

public class AssignRoleEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/users/staff/{userId:guid}/role",
            async (Guid userId, AssignRoleRequest request, HttpContext http, ISender sender, CancellationToken ct) =>
            {
                var restaurantId = http.User.GetRestaurantId()
                    ?? throw new UnauthorizedAccessException("No restaurant context.");
                await sender.Send(new AssignRoleCommand(restaurantId, userId, request.RoleId), ct);
                return Results.NoContent();
            })
        .WithName("AssignStaffRole")
        .WithTags("Users")
        .RequirePermission(Permissions.UsersManage);
    }
}
