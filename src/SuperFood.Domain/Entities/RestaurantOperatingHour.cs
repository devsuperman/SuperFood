namespace SuperFood.Domain.Entities;

/// <summary>One weekday's operating window (US-0202). IsClosed overrides the times.</summary>
public class RestaurantOperatingHour
{
    public Guid Id { get; set; }
    public Guid RestaurantId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly OpenTime { get; set; }
    public TimeOnly CloseTime { get; set; }
    public bool IsClosed { get; set; }
}
