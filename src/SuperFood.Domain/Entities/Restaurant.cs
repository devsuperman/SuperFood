using SuperFood.Domain.Enums;

namespace SuperFood.Domain.Entities;

/// <summary>Tenant root. EPIC-01/EPIC-02.</summary>
public class Restaurant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? Address { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public RestaurantStatus Status { get; set; } = RestaurantStatus.Active;
    public bool DineInEnabled { get; set; } = true;
    public bool DeliveryEnabled { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<RestaurantOperatingHour> OperatingHours { get; set; } = [];
    public List<Role> Roles { get; set; } = [];
    public List<Category> Categories { get; set; } = [];
    public List<Table> Tables { get; set; } = [];
    public List<Order> Orders { get; set; } = [];
}
