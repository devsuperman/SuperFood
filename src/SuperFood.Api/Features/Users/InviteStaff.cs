using System.Security.Cryptography;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SuperFood.Api.Common;
using SuperFood.Api.Common.Exceptions;
using SuperFood.Contracts.Users;
using SuperFood.Domain;
using SuperFood.Domain.Entities;
using SuperFood.Infrastructure.Identity;
using SuperFood.Infrastructure.Persistence;

namespace SuperFood.Api.Features.Users;

/// <summary>US-0301: restaurant_owner invites a staff member by email.</summary>
public record InviteStaffCommand(Guid RestaurantId, string FullName, string Email, Guid RoleId)
    : IRequest<InviteStaffResponse>;

public class InviteStaffValidator : AbstractValidator<InviteStaffCommand>
{
    public InviteStaffValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}

public class InviteStaffHandler(SuperFoodDbContext db, UserManager<ApplicationUser> userManager)
    : IRequestHandler<InviteStaffCommand, InviteStaffResponse>
{
    public async Task<InviteStaffResponse> Handle(InviteStaffCommand request, CancellationToken cancellationToken)
    {
        var roleExists = await db.Set<Role>().AnyAsync(r => r.Id == request.RoleId, cancellationToken);
        if (!roleExists)
        {
            throw new NotFoundException($"Role '{request.RoleId}' was not found.");
        }

        // No email service in this iteration (docs/user-stories.md §4): return
        // a temporary password the owner relays to the new staff member.
        var temporaryPassword = $"{Convert.ToBase64String(RandomNumberGenerator.GetBytes(9))}!1Aa";

        var staff = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            RestaurantId = request.RestaurantId,
            RoleId = request.RoleId,
            IsActive = true,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(staff, temporaryPassword);
        if (!result.Succeeded)
        {
            throw new ValidationException(result.Errors
                .Select(e => new FluentValidation.Results.ValidationFailure(nameof(request.Email), e.Description)));
        }

        return new InviteStaffResponse(staff.Id, staff.Email!, temporaryPassword);
    }
}

public class InviteStaffEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/users/staff",
            async (InviteStaffRequest request, HttpContext http, ISender sender, CancellationToken ct) =>
            {
                var restaurantId = http.User.GetRestaurantId()
                    ?? throw new UnauthorizedAccessException("No restaurant context.");
                var result = await sender.Send(
                    new InviteStaffCommand(restaurantId, request.FullName, request.Email, request.RoleId), ct);
                return Results.Created($"/api/users/staff/{result.UserId}", result);
            })
        .WithName("InviteStaff")
        .WithTags("Users")
        .RequirePermission(Permissions.UsersManage);
    }
}
