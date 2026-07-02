using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using WMS.Application.Skus.Queries.SkuLookup;
using WMS.Domain.Entities;
using WMS.Domain.Entities.SkuAggregateRoot;
using WMS.Domain.Interfaces;
using WMS.Infrastructure.Persistence;

namespace DP.AppWMS.Tests.Skus;

public sealed class SkuLookupQueryHandlerTests : BaseSkuHandlerTest
{
    [Fact]
    public async Task Handle_WhenSkusExist_ReturnsAllActiveSkusForTenant()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        // Seed data
        await AddTestSku(db, TenantA, "SKU-A1", "Sku A1");
        await AddTestSku(db, TenantA, "SKU-A2", "Sku A2");
        var deletedSku = await AddTestSku(db, TenantA, "SKU-DELETED", "Deleted Sku");
        deletedSku.MarkDeleted();
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await AddTestSku(db, TenantB, "SKU-B1", "Sku B1");

        var handler = CreateHandler(db);

        // Act
        var result = await handler.Handle(new SkuLookupQuery(TenantA), TestContext.Current.CancellationToken);

        // Assert
        result.Should().HaveCount(2);
        result.Select(x => x.Code).Should().ContainInOrder("SKU-A1", "SKU-A2");
        result.Select(x => x.Code).Should().NotContain("SKU-DELETED");
        result.Select(x => x.Code).Should().NotContain("SKU-B1");
    }

    private static SkuLookupQueryHandler CreateHandler(WmsDbContext db)
    {
        var skuRepo = new Mock<IRepository<Sku>>();
        skuRepo.Setup(x => x.Query()).Returns(db.Skus);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.Repository<Sku>()).Returns(skuRepo.Object);

        return new SkuLookupQueryHandler(uow.Object);
    }
}
