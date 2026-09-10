namespace SuperFood.Domain.Entities;

/// <summary>EPIC-05: a sellable menu item.</summary>
public class Product
{
    public Guid Id { get; set; }
    public Guid RestaurantId { get; set; }
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? PhotoUrl { get; set; }
    public bool IsAvailable { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    public Category? Category { get; set; }
    public List<ProductVariation> Variations { get; set; } = [];
}
