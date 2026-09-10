using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SuperFood.Infrastructure.RealTime;

/// <summary>
/// Pushes kitchen/waiter order events (US-0901/US-0902/US-0903). Clients join
/// a group named after their restaurant_id claim, the tenant isolation
/// boundary for real-time messages (docs/tech-stack.md §7).
/// </summary>
[Authorize]
public class OrdersHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var restaurantId = Context.User?.FindFirst("restaurant_id")?.Value;
        if (!string.IsNullOrEmpty(restaurantId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(restaurantId));
        }

        await base.OnConnectedAsync();
    }

    public static string GroupName(string restaurantId) => $"restaurant:{restaurantId}";
}
