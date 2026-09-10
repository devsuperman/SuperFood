using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperFood.Domain.Entities;

namespace SuperFood.Infrastructure.Persistence.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.Property(i => i.ProductName).HasMaxLength(200).IsRequired();
        builder.Property(i => i.VariationName).HasMaxLength(100);
        builder.Property(i => i.UnitPrice).HasPrecision(10, 2);
        builder.Property(i => i.VariationPriceAdjustment).HasPrecision(10, 2);
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(i => i.RestaurantId);
        builder.HasIndex(i => i.OrderId);
    }
}
