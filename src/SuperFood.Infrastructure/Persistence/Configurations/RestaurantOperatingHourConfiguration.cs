using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperFood.Domain.Entities;

namespace SuperFood.Infrastructure.Persistence.Configurations;

public class RestaurantOperatingHourConfiguration : IEntityTypeConfiguration<RestaurantOperatingHour>
{
    public void Configure(EntityTypeBuilder<RestaurantOperatingHour> builder)
    {
        builder.HasIndex(h => new { h.RestaurantId, h.DayOfWeek }).IsUnique();
    }
}
