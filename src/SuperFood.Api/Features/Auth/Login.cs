using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SuperFood.Api.Common;
using SuperFood.Contracts.Auth;
using SuperFood.Domain.Entities;
using SuperFood.Infrastructure.Auth;
using SuperFood.Infrastructure.Identity;
using SuperFood.Infrastructure.Persistence;

namespace SuperFood.Api.Features.Auth;

public record LoginCommand(string Email, string Password) : IRequest<LoginResponse>;

public class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class LoginHandler(
    UserManager<ApplicationUser> userManager,
    SuperFoodDbContext db,
    IJwtTokenService tokenService) : IRequestHandler<LoginCommand, LoginResponse>
{
    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        if (user.RestaurantId is { } userRestaurantId)
        {
            // US-0103 AC: a suspended restaurant's staff can no longer sign in.
            var restaurant = await db.Restaurants.IgnoreQueryFilters()
                .FirstOrDefaultAsync(r => r.Id == userRestaurantId, cancellationToken);
            if (restaurant is null || restaurant.Status == Domain.Enums.RestaurantStatus.Suspended)
            {
                throw new UnauthorizedAccessException("This restaurant's account is suspended.");
            }
        }

        string[] permissions = [];
        if (!user.IsPlatformAdmin && user.RoleId is { } roleId)
        {
            // No tenant claim exists yet during login, so bypass the filter explicitly.
            var role = await db.Set<Role>().IgnoreQueryFilters()
                .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);
            permissions = role?.Permissions ?? [];
        }

        var accessToken = tokenService.GenerateAccessToken(user, permissions);
        var (rawRefreshToken, hash, expiresAt) = tokenService.GenerateRefreshToken();

        db.Set<RefreshToken>().Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = hash,
            ExpiresAt = expiresAt
        });
        await db.SaveChangesAsync(cancellationToken);

        return new LoginResponse(
            accessToken.Value,
            accessToken.ExpiresAt,
            rawRefreshToken,
            user.Id,
            user.FullName,
            user.RestaurantId,
            user.IsPlatformAdmin,
            permissions);
    }
}

public class LoginEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", async (LoginRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new LoginCommand(request.Email, request.Password), ct);
            return Results.Ok(result);
        })
        .WithName("Login")
        .WithTags("Auth")
        .AllowAnonymous();
    }
}
