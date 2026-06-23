using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Suppliers.Queries.SearchSuppliers;
using WMS.Domain.Entities.Master;
using WMS.Infrastructure.Persistence;

namespace DP.AppWMS.Tests.Suppliers;

public sealed class SearchSuppliersQueryHandlerTests : BaseSupplierHandlerTest
{
    private static SearchSuppliersQueryHandler CreateHandler(WmsDbContext db)
    {
        return new SearchSuppliersQueryHandler(CreateUnitOfWork(db));
    }

    [Fact]
    public async Task Handle_WithNoFilters_ReturnsActiveSuppliersOrderedByName()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var s1 = Supplier.Create(TenantA, "SUP01", "Beta Supplier");
        var s2 = Supplier.Create(TenantA, "SUP02", "Alpha Supplier");
        var s3 = Supplier.Create(TenantA, "SUP03", "Deleted Supplier");
        s3.Delete("admin");

        db.Set<Supplier>().AddRange(s1, s2, s3);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var query = new SearchSuppliersQuery(TenantA, null, 1, 10);

        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Items[0].Name.Should().Be("Alpha Supplier"); // Ordered by name ascending
        result.Items[1].Name.Should().Be("Beta Supplier");
    }

    [Fact]
    public async Task Handle_WithSearchTerm_FiltersByCodeOrName()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var s1 = Supplier.Create(TenantA, "SUP01", "Apple");
        var s2 = Supplier.Create(TenantA, "BANANA", "Banana Inc");
        db.Set<Supplier>().AddRange(s1, s2);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);

        // Search by name contains
        var query1 = new SearchSuppliersQuery(TenantA, "ppl", 1, 10);
        var result1 = await handler.Handle(query1, TestContext.Current.CancellationToken);
        result1.Items.Should().ContainSingle(x => x.Code == "SUP01");

        // Search by code case-insensitive
        var query2 = new SearchSuppliersQuery(TenantA, "banana", 1, 10);
        var result2 = await handler.Handle(query2, TestContext.Current.CancellationToken);
        result2.Items.Should().ContainSingle(x => x.Name == "Banana Inc");
    }

}
