namespace SuperFood.Domain.Entities;

/// <summary>Groups the dine-in orders for one seating (US-0604/US-0605).</summary>
public class TableSession
{
    public Guid Id { get; set; }
    public Guid RestaurantId { get; set; }
    public Guid TableId { get; set; }
    public DateTimeOffset OpenedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ClosedAt { get; set; }

    public Table? Table { get; set; }
    public List<Order> Orders { get; set; } = [];

    public bool IsOpen => ClosedAt is null;
}
