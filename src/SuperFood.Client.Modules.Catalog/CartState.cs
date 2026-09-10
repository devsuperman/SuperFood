namespace SuperFood.Client.Modules.Catalog;

public class CartLine
{
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public Guid? VariationId { get; init; }
    public string? VariationName { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal VariationPriceAdjustment { get; init; }
    public int Quantity { get; set; } = 1;
    public string? Notes { get; set; }

    public decimal LineTotal => (UnitPrice + VariationPriceAdjustment) * Quantity;
}

/// <summary>
/// Scoped cart state for the customer ordering flow (US-1003: review cart
/// before submitting). Registered directly in the host since Catalog loads
/// eagerly (docs/tech-stack.md §9.3) - no lazy-loading boundary to protect here.
/// </summary>
public class CartState
{
    public List<CartLine> Lines { get; } = [];

    public event Action? Changed;

    public void Add(CartLine line)
    {
        var existing = Lines.FirstOrDefault(l => l.ProductId == line.ProductId && l.VariationId == line.VariationId
            && l.Notes == line.Notes);
        if (existing is not null)
        {
            existing.Quantity += line.Quantity;
        }
        else
        {
            Lines.Add(line);
        }

        Changed?.Invoke();
    }

    public void Remove(CartLine line)
    {
        Lines.Remove(line);
        Changed?.Invoke();
    }

    public void Clear()
    {
        Lines.Clear();
        Changed?.Invoke();
    }

    public decimal Total => Lines.Sum(l => l.LineTotal);
}
