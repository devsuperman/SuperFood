using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperFood.Api.Common;
using SuperFood.Api.Common.Exceptions;
using SuperFood.Domain;
using SuperFood.Infrastructure.Identity;
using SuperFood.Infrastructure.Persistence;

namespace SuperFood.Api.Features.Users;

/// <summary>US-0304: deactivate a staff member's access without deleting their history.</summary>
public record DeactivateStaffCommand(Guid RestaurantId, Guid UserId) : IRequest;

public class DeactivateStaffHandler(SuperFoodDbContext db) : IRequestHandler<DeactivateStaffCommand>
{
    public async Task Handle(DeactivateStaffCommand request, CancellationToken cancellationToken)
    {
        var user = await db.Set<ApplicationUser>()
            .FirstOrDefaultAsync(u => u.Id == request.UserId && u.RestaurantId == request.RestaurantId, cancellationToken)
            ?? throw new NotFoundException($"Staff member '{request.UserId}' was not found.");

        user.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);
    }
}

public class DeactivateStaffEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/users/staff/{userId:guid}/deactivate",
            async (Guid userId, HttpContext http, ISender sender, CancellationToken ct) =>
            {
                var restaurantId = http.User.GetRestaurantId()
                    ?? throw new UnauthorizedAccessException("No restaurant context.");
                await sender.Send(new DeactivateStaffCommand(restaurantId, userId), ct);
                return Results.NoContent();
            })
        .WithName("DeactivateStaff")
        .WithTags("Users")
        .RequirePermission(Permissions.UsersManage);
    }
}
