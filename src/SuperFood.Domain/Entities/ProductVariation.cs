namespace SuperFood.Domain.Entities;

/// <summary>A customization option for a product (e.g. size), US-0502.</summary>
public class ProductVariation
{
    public Guid Id { get; set; }
    public Guid RestaurantId { get; set; }
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal PriceAdjustment { get; set; }
}
