namespace SuperFood.Domain.Entities;

/// <summary>A restaurant-defined role granting a subset of <see cref="Permissions"/> (US-0302/US-0305).</summary>
public class Role
{
    public Guid Id { get; set; }
    public Guid RestaurantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string[] Permissions { get; set; } = [];
}
