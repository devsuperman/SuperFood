using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace SuperFood.Client.Auth;

/// <summary>
/// Builds the Blazor <see cref="ClaimsPrincipal"/> straight from the stored
/// <see cref="AuthSession"/> - the JWT itself is only ever presented to the
/// API (which validates it); the client trusts its own just-stored session.
/// </summary>
public class ApiAuthenticationStateProvider(AuthSessionStore sessionStore) : AuthenticationStateProvider
{
    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var session = await sessionStore.GetAsync();
        if (session is null || session.AccessTokenExpiresAt <= DateTimeOffset.UtcNow)
        {
            return new AuthenticationState(Anonymous);
        }

        return new AuthenticationState(BuildPrincipal(session));
    }

    public void NotifySignedIn(AuthSession session) =>
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(BuildPrincipal(session))));

    public void NotifySignedOut() =>
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(Anonymous)));

    private static ClaimsPrincipal BuildPrincipal(AuthSession session)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, session.UserId.ToString()),
            new("full_name", session.FullName),
            new("is_platform_admin", session.IsPlatformAdmin ? "true" : "false")
        };

        if (session.RestaurantId is { } restaurantId)
        {
            claims.Add(new Claim("restaurant_id", restaurantId.ToString()));
        }

        claims.AddRange(session.Permissions.Select(p => new Claim("permission", p)));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "SuperFood"));
    }
}
