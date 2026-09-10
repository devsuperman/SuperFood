namespace SuperFood.Client.Auth;

/// <summary>What's kept in browser local storage between page loads (docs/tech-stack.md §9.4).</summary>
public record AuthSession(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    Guid UserId,
    string FullName,
    Guid? RestaurantId,
    bool IsPlatformAdmin,
    string[] Permissions);
