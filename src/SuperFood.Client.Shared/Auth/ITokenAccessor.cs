namespace SuperFood.Client.Auth;

/// <summary>
/// The minimal auth surface exposed to feature modules (docs/tech-stack.md
/// §9.2) - e.g. for attaching the access token to a SignalR connection,
/// which needs the raw token rather than the derived ClaimsPrincipal.
/// </summary>
public interface ITokenAccessor
{
    Task<string?> GetAccessTokenAsync();
}

public class TokenAccessor(AuthSessionStore sessionStore) : ITokenAccessor
{
    public async Task<string?> GetAccessTokenAsync() => (await sessionStore.GetAsync())?.AccessToken;
}
