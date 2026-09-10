using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperFood.Api.Common;
using SuperFood.Api.Common.Exceptions;
using SuperFood.Contracts.Users;
using SuperFood.Domain;
using SuperFood.Domain.Entities;
using SuperFood.Infrastructure.Persistence;

namespace SuperFood.Api.Features.Users;

/// <summary>US-0305: edit a role's permissions as the operation evolves.</summary>
public record UpdateRoleCommand(Guid RestaurantId, Guid RoleId, string Name, string[] Permissions) : IRequest;

public class UpdateRoleValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleForEach(x => x.Permissions).Must(p => SuperFood.Domain.Permissions.All.Contains(p))
            .WithMessage("Unknown permission.");
    }
}

public class UpdateRoleHandler(SuperFoodDbContext db) : IRequestHandler<UpdateRoleCommand>
{
    public async Task Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await db.Set<Role>()
            .FirstOrDefaultAsync(r => r.Id == request.RoleId && r.RestaurantId == request.RestaurantId, cancellationToken)
            ?? throw new NotFoundException($"Role '{request.RoleId}' was not found.");

        role.Name = request.Name;
        role.Permissions = request.Permissions;
        await db.SaveChangesAsync(cancellationToken);
    }
}

public class UpdateRoleEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/users/roles/{roleId:guid}",
            async (Guid roleId, UpdateRoleRequest request, HttpContext http, ISender sender, CancellationToken ct) =>
            {
                var restaurantId = http.User.GetRestaurantId()
                    ?? throw new UnauthorizedAccessException("No restaurant context.");
                await sender.Send(new UpdateRoleCommand(restaurantId, roleId, request.Name, request.Permissions), ct);
                return Results.NoContent();
            })
        .WithName("UpdateRole")
        .WithTags("Users")
        .RequirePermission(Permissions.UsersManage);
    }
}
