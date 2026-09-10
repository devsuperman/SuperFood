using FluentValidation;
using MediatR;
using SuperFood.Api.Common;
using SuperFood.Contracts.Users;
using SuperFood.Domain;
using SuperFood.Domain.Entities;
using SuperFood.Infrastructure.Persistence;

namespace SuperFood.Api.Features.Users;

/// <summary>US-0302: define a custom role with a subset of the fixed permission set.</summary>
public record CreateRoleCommand(Guid RestaurantId, string Name, string[] Permissions) : IRequest<RoleResponse>;

public class CreateRoleValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleForEach(x => x.Permissions).Must(p => SuperFood.Domain.Permissions.All.Contains(p))
            .WithMessage("Unknown permission.");
    }
}

public class CreateRoleHandler(SuperFoodDbContext db) : IRequestHandler<CreateRoleCommand, RoleResponse>
{
    public async Task<RoleResponse> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = new Role
        {
            Id = Guid.NewGuid(),
            RestaurantId = request.RestaurantId,
            Name = request.Name,
            Permissions = request.Permissions
        };

        db.Set<Role>().Add(role);
        await db.SaveChangesAsync(cancellationToken);

        return new RoleResponse(role.Id, role.Name, role.Permissions);
    }
}

public class CreateRoleEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/users/roles",
            async (CreateRoleRequest request, HttpContext http, ISender sender, CancellationToken ct) =>
            {
                var restaurantId = http.User.GetRestaurantId()
                    ?? throw new UnauthorizedAccessException("No restaurant context.");
                var result = await sender.Send(new CreateRoleCommand(restaurantId, request.Name, request.Permissions), ct);
                return Results.Created($"/api/users/roles/{result.Id}", result);
            })
        .WithName("CreateRole")
        .WithTags("Users")
        .RequirePermission(Permissions.UsersManage);
    }
}
