using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WMS.Domain.Entities.SkuAggregateRoot;
using WMS.Domain.Interfaces;
using WMS.Infrastructure.Persistence;
using ProductAggregate = WMS.Domain.Entities.ProductAggregateRoot.Product;

namespace DP.AppWMS.Tests.Skus
{
    public abstract class BaseSkuHandlerTest
    {
        protected static readonly Guid TenantA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        protected static readonly Guid TenantB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        protected static readonly DateTime BaseTime = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Creates separate Product and Sku aggregate rows and saves both to the database.
        /// Returns the Sku entity for assertions.
        /// </summary>
        protected static async Task<Sku> AddTestSku(
            WmsDbContext db,
            Guid tenantId,
            string skuCode,
            string name,
            string? description = null,
            decimal? referencePrice = null,
            Guid? categoryId = null,
            CancellationToken ct = default)
        {
            var product = ProductAggregate.Create(
                tenantId,
                $"PROD-{skuCode}",
                $"{name} Product",
                categoryId: categoryId);
            db.Set<ProductAggregate>().Add(product);
            await db.SaveChangesAsync(ct);

            var sku = Sku.Create(
                tenantId,
                product.Id,
                skuCode,
                name,
                null,
                description,
                referencePrice ?? 0m);
            db.Set<Sku>().Add(sku);
            await db.SaveChangesAsync(ct);

            return sku;
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

            return new WmsDbContext(options, Mock.Of<ICurrentUser>(), Mock.Of<MediatR.IMediator>());
        }

        protected static UnitOfWork CreateUnitOfWork(WmsDbContext db)
        {
            return new UnitOfWork(db, NullLogger<UnitOfWork>.Instance);
        }

    }
}
