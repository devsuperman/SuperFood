using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SuperFood.Domain.Entities;
using SuperFood.Infrastructure.Identity;

namespace SuperFood.Infrastructure.Persistence;

/// <summary>
/// Single DbContext for the whole app (docs/tech-stack.md §6). Uses
/// IdentityUserContext (not the Role-bearing base) because roles/permissions
/// are modeled as the domain's own <see cref="Role"/> entity, not ASP.NET
/// Identity roles (ADR-06).
/// </summary>
public class SuperFoodDbContext(DbContextOptions<SuperFoodDbContext> options, ICurrentTenantProvider tenantProvider)
    : IdentityUserContext<ApplicationUser, Guid>(options)
{
    public DbSet<Restaurant> Restaurants => Set<Restaurant>();
    public DbSet<RestaurantOperatingHour> RestaurantOperatingHours => Set<RestaurantOperatingHour>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariation> ProductVariations => Set<ProductVariation>();
    public DbSet<Table> Tables => Set<Table>();
    public DbSet<TableSession> TableSessions => Set<TableSession>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(SuperFoodDbContext).Assembly);

        // Npgsql maps string[] to a native Postgres text[] column; SQLite (used
        // by SuperFood.UnitTests, docs/tech-stack.md §10) has no array type, so
        // unit tests get a delimited-string fallback instead.
        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            builder.Entity<Role>().Property(r => r.Permissions)
                .HasConversion(
                    v => string.Join('|', v),
                    v => v.Split('|', StringSplitOptions.RemoveEmptyEntries));
        }

        // Multi-tenancy (docs/tech-stack.md §4): platform_admin bypasses via
        // IgnoreQueryFilters() at the query site; everyone else is scoped to
        // their own restaurant_id claim.
        builder.Entity<Restaurant>().HasQueryFilter(r =>
            tenantProvider.IsPlatformAdmin || r.Id == tenantProvider.RestaurantId);
        builder.Entity<Role>().HasQueryFilter(r =>
            tenantProvider.IsPlatformAdmin || r.RestaurantId == tenantProvider.RestaurantId);
        builder.Entity<Category>().HasQueryFilter(c =>
            tenantProvider.IsPlatformAdmin || c.RestaurantId == tenantProvider.RestaurantId);
        builder.Entity<Product>().HasQueryFilter(p =>
            tenantProvider.IsPlatformAdmin || p.RestaurantId == tenantProvider.RestaurantId);
        builder.Entity<ProductVariation>().HasQueryFilter(v =>
            tenantProvider.IsPlatformAdmin || v.RestaurantId == tenantProvider.RestaurantId);
        builder.Entity<Table>().HasQueryFilter(t =>
            tenantProvider.IsPlatformAdmin || t.RestaurantId == tenantProvider.RestaurantId);
        builder.Entity<TableSession>().HasQueryFilter(s =>
            tenantProvider.IsPlatformAdmin || s.RestaurantId == tenantProvider.RestaurantId);
        builder.Entity<Order>().HasQueryFilter(o =>
            tenantProvider.IsPlatformAdmin || o.RestaurantId == tenantProvider.RestaurantId);
        builder.Entity<OrderItem>().HasQueryFilter(i =>
            tenantProvider.IsPlatformAdmin || i.RestaurantId == tenantProvider.RestaurantId);

        // No query filter on ApplicationUser: ASP.NET Identity's UserManager
        // looks users up (e.g. by email at login) before any tenant claim
        // exists on the request, so a tenant-scoped filter here would hide
        // every staff member from the login query itself. Users slices
        // (EPIC-03) filter by restaurant explicitly instead.
    }
}
