using SuperFood.Infrastructure.Identity;

namespace SuperFood.Infrastructure.Auth;

public record AccessToken(string Value, DateTimeOffset ExpiresAt);

public interface IJwtTokenService
{
    AccessToken GenerateAccessToken(ApplicationUser user, IReadOnlyCollection<string> permissions);

    /// <summary>Returns the raw refresh token (given to the client) and its hash (persisted).</summary>
    (string RawToken, string Hash, DateTimeOffset ExpiresAt) GenerateRefreshToken();

    string HashRefreshToken(string rawToken);
}
