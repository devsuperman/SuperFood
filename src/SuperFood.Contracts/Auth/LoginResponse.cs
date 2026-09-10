namespace SuperFood.Contracts.Auth;

public record LoginResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    Guid UserId,
    string FullName,
    Guid? RestaurantId,
    bool IsPlatformAdmin,
    string[] Permissions);
