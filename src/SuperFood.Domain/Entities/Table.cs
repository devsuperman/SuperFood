using SuperFood.Domain.Enums;

namespace SuperFood.Domain.Entities;

/// <summary>EPIC-06: a physical dine-in table.</summary>
public class Table
{
    public Guid Id { get; set; }
    public Guid RestaurantId { get; set; }
    public string Identifier { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string QrCodeToken { get; set; } = Guid.NewGuid().ToString("N");
    public TableStatus Status { get; set; } = TableStatus.Free;

    public List<TableSession> Sessions { get; set; } = [];
}
