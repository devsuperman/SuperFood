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

public record RefreshTokenCommand(string RefreshToken) : IRequest<LoginResponse>;

public class RefreshTokenValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}

public class RefreshTokenHandler(
    SuperFoodDbContext db,
    UserManager<ApplicationUser> userManager,
    IJwtTokenService tokenService) : IRequestHandler<RefreshTokenCommand, LoginResponse>
{
    public async Task<LoginResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var hash = tokenService.HashRefreshToken(request.RefreshToken);
        var existing = await db.Set<Infrastructure.Identity.RefreshToken>()
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (existing is null || !existing.IsActive)
        {
            throw new UnauthorizedAccessException("Refresh token is invalid or expired.");
        }

        var user = await userManager.FindByIdAsync(existing.UserId.ToString());
        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedAccessException("User is no longer active.");
        }

        // Rotate: revoke the used token and issue a new one.
        existing.RevokedAt = DateTimeOffset.UtcNow;

        string[] permissions = [];
        if (!user.IsPlatformAdmin && user.RoleId is { } roleId)
        {
            var role = await db.Set<Role>().IgnoreQueryFilters()
                .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);
            permissions = role?.Permissions ?? [];
        }

        var accessToken = tokenService.GenerateAccessToken(user, permissions);
        var (rawRefreshToken, newHash, expiresAt) = tokenService.GenerateRefreshToken();

        db.Set<Infrastructure.Identity.RefreshToken>().Add(new Infrastructure.Identity.RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = newHash,
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

public class RefreshTokenEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/refresh", async (RefreshTokenRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RefreshTokenCommand(request.RefreshToken), ct);
            return Results.Ok(result);
        })
        .WithName("RefreshToken")
        .WithTags("Auth")
        .AllowAnonymous();
    }
}
