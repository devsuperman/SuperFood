namespace SuperFood.Api.Common;

public static class RouteHandlerBuilderExtensions
{
    /// <summary>
    /// Requires the caller to hold the given permission claim (ADR-06). Policy
    /// names equal the permission string — see Program.cs where one policy per
    /// entry in <see cref="SuperFood.Domain.Permissions.All"/> is registered.
    /// </summary>
    public static RouteHandlerBuilder RequirePermission(this RouteHandlerBuilder builder, string permission) =>
        builder.RequireAuthorization(permission);

    /// <summary>Requires the caller to be a platform_admin (EPIC-01).</summary>
    public static RouteHandlerBuilder RequirePlatformAdmin(this RouteHandlerBuilder builder) =>
        builder.RequireAuthorization("platform_admin");
}
