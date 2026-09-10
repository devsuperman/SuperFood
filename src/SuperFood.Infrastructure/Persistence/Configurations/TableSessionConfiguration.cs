using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperFood.Domain.Entities;

namespace SuperFood.Infrastructure.Persistence.Configurations;

public class TableSessionConfiguration : IEntityTypeConfiguration<TableSession>
{
    public void Configure(EntityTypeBuilder<TableSession> builder)
    {
        builder.HasIndex(s => s.RestaurantId);
        builder.HasIndex(s => s.TableId);

        builder.HasMany(s => s.Orders)
            .WithOne(o => o.TableSession)
            .HasForeignKey(o => o.TableSessionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
