using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperFood.Domain.Entities;

namespace SuperFood.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.Property(o => o.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(o => o.PaymentStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(o => o.PaymentMethod).HasConversion<string>().HasMaxLength(20);
        builder.Ignore(o => o.Total);
        builder.HasIndex(o => o.RestaurantId);
        builder.HasIndex(o => o.CustomerSessionId);

        builder.OwnsOne(o => o.DeliveryDetails, d =>
        {
            d.Property(p => p.CustomerName).HasColumnName("delivery_customer_name").HasMaxLength(150);
            d.Property(p => p.Address).HasColumnName("delivery_address").HasMaxLength(300);
            d.Property(p => p.ContactPhone).HasColumnName("delivery_contact_phone").HasMaxLength(30);
        });

        builder.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
