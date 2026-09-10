namespace SuperFood.Domain;

/// <summary>
/// Fixed, code-defined set of permissions a restaurant's custom Role (US-0302)
/// can grant. Endpoints require one of these rather than a role name, since
/// role names are restaurant-defined (see docs/tech-stack.md ADR-06).
/// </summary>
public static class Permissions
{
    public const string RestaurantManage = "restaurant:manage";
    public const string UsersManage = "users:manage";
    public const string MenuManage = "menu:manage";
    public const string TablesManage = "tables:manage";
    public const string OrdersManage = "orders:manage";
    public const string KitchenManage = "kitchen:manage";

    public static readonly IReadOnlyList<string> All =
    [
        RestaurantManage,
        UsersManage,
        MenuManage,
        TablesManage,
        OrdersManage,
        KitchenManage
    ];
}
