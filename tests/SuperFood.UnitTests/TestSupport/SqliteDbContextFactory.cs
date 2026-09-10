using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SuperFood.Infrastructure.Persistence;

namespace SuperFood.UnitTests.TestSupport;

/// <summary>
/// An in-memory SQLite connection kept open for the test's lifetime (SQLite's
/// in-memory mode drops the database as soon as the connection closes).
/// </summary>
public sealed class SqliteDbContextFactory : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteDbContextFactory()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();
    }

    public SuperFoodDbContext CreateContext(ICurrentTenantProvider? tenantProvider = null)
    {
        var options = new DbContextOptionsBuilder<SuperFoodDbContext>()
            .UseSqlite(_connection)
            .Options;

        var context = new SuperFoodDbContext(options, tenantProvider ?? new FakeCurrentTenantProvider(null, isPlatformAdmin: true));
        context.Database.EnsureCreated();
        return context;
    }

    public void Dispose() => _connection.Dispose();
}
