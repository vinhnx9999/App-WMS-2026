using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using WMS.Application.Suppliers.Queries.SupplierLookup;
using WMS.Domain.Entities.Master;
using WMS.Domain.Interfaces;
using WMS.Infrastructure.Persistence;

namespace DP.AppWMS.Tests.Suppliers;

public sealed class SupplierLookupQueryHandlerTests : BaseSupplierHandlerTest
{
    [Fact]
    public async Task Handle_WhenSuppliersExist_ReturnsAllActiveSuppliersForTenant()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        // Seed data
        var s1 = Supplier.Create(TenantA, "SUP-01", "Supplier 1");
        var s2 = Supplier.Create(TenantA, "SUP-02", "Supplier 2");
        var sDeleted = Supplier.Create(TenantA, "SUP-DEL", "Deleted Supplier");
        sDeleted.Delete("admin");
        var sOther = Supplier.Create(TenantB, "SUP-B1", "Supplier B1");

        db.Set<Supplier>().AddRange(s1, s2, sDeleted, sOther);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);

        // Act
        var result = await handler.Handle(new SupplierLookupQuery(TenantA), TestContext.Current.CancellationToken);

        // Assert
        result.Should().HaveCount(2);
        result.Select(x => x.Code).Should().ContainInOrder("SUP-01", "SUP-02");
        result.Select(x => x.Code).Should().NotContain("SUP-DEL");
        result.Select(x => x.Code).Should().NotContain("SUP-B1");
    }

    private static SupplierLookupQueryHandler CreateHandler(WmsDbContext db)
    {
        var supplierRepo = new Mock<IRepository<Supplier>>();
        supplierRepo.Setup(x => x.Query()).Returns(db.Suppliers);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.Repository<Supplier>()).Returns(supplierRepo.Object);

        return new SupplierLookupQueryHandler(uow.Object);
    }
}
