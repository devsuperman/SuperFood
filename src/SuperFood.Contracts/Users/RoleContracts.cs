namespace SuperFood.Contracts.Users;

public record CreateRoleRequest(string Name, string[] Permissions);

public record UpdateRoleRequest(string Name, string[] Permissions);

public record RoleResponse(Guid Id, string Name, string[] Permissions);

/// <summary>
/// Mirrors the fixed permission set in SuperFood.Domain.Permissions - kept
/// here too since the client only references Contracts, not the
/// server-only Domain project (docs/tech-stack.md §2).
/// </summary>
public static class AvailablePermissions
{
    public const string RestaurantManage = "restaurant:manage";
    public const string UsersManage = "users:manage";
    public const string MenuManage = "menu:manage";
    public const string TablesManage = "tables:manage";
    public const string OrdersManage = "orders:manage";
    public const string KitchenManage = "kitchen:manage";

    public static readonly IReadOnlyList<string> All =
    [
        RestaurantManage, UsersManage, MenuManage, TablesManage, OrdersManage, KitchenManage
    ];
}
