using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WMS.Domain.Entities;
using WMS.Infrastructure.Persistence;

namespace DP.AppWMS.Tests.Skus
{
    public abstract class BaseSkuHandlerTest
    {
        protected static readonly Guid TenantA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        protected static readonly Guid TenantB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        protected static readonly DateTime BaseTime = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        protected static Category CreateCategory(Guid tenantId, string name)
        {
            return new Category
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = name,
                CreatedAt = BaseTime,
                UpdatedAt = BaseTime,
            };
        }

        protected static SkuEntity CreateSku(
            Guid tenantId,
            string skuCode,
            string name,
            Guid? categoryId = null,
            string? description = "Description",
            decimal? price = 10,
            DateTime? createdAt = null,
            DateTime? updatedAt = null,
            DateTime? deleteAt = null)
        {
            return new SkuEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                CategoryId = categoryId,
                SkuCode = skuCode,
                Name = name,
                Description = description,
                Price = price,
                CreatedAt = createdAt ?? BaseTime,
                UpdatedAt = updatedAt ?? BaseTime,
                DeletedAt = deleteAt,
            };
        }

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

            return new WmsDbContext(options);
        }

        protected static UnitOfWork CreateUnitOfWork(WmsDbContext db)
        {
            return new UnitOfWork(db, NullLogger<UnitOfWork>.Instance);
        }

    }
}
