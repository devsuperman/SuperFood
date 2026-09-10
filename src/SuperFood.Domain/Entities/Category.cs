namespace SuperFood.Domain.Entities;

/// <summary>EPIC-04: menu category.</summary>
public class Category
{
    public Guid Id { get; set; }
    public Guid RestaurantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsVisible { get; set; } = true;

    public List<Product> Products { get; set; } = [];
}
