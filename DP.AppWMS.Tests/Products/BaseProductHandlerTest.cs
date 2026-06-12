using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WMS.Domain.Entities;
using WMS.Domain.Interfaces;
using WMS.Infrastructure.Persistence;

namespace DP.AppWMS.Tests.Products;

public abstract class BaseProductHandlerTest
{
    protected static readonly Guid TenantA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    protected static readonly Guid TenantB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    protected static async Task<SqliteConnection> OpenConnectionAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        return connection;
    }

    protected static WmsDbContext CreateDbContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<WmsDbContext>()
            .UseSqlite(connection)
            .Options;

        return new WmsDbContext(options, Mock.Of<ICurrentUser>());
    }

    protected static UnitOfWork CreateUnitOfWork(WmsDbContext db)
    {
        return new UnitOfWork(db, NullLogger<UnitOfWork>.Instance);
    }

    protected static async Task<Category> AddTestCategory(
        WmsDbContext db,
        Guid tenantId,
        string name,
        CancellationToken ct = default)
    {
        var category = Category.Create(tenantId, name);
        db.Set<Category>().Add(category);
        await db.SaveChangesAsync(ct);
        return category;
    }
}
