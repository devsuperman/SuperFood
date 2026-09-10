namespace SuperFood.Domain.Entities;

/// <summary>Owned by a delivery Order (US-0801). Not a customer account — captured per order.</summary>
public class DeliveryDetails
{
    public string CustomerName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
}
