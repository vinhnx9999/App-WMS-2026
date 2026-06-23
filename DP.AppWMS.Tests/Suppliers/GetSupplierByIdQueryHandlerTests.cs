using FluentAssertions;
using WMS.Application.Common.Models;
using WMS.Application.Suppliers.Queries.GetSupplierById;
using WMS.Domain.Entities.Master;
using WMS.Infrastructure.Persistence;

namespace DP.AppWMS.Tests.Suppliers;

public sealed class GetSupplierByIdQueryHandlerTests : BaseSupplierHandlerTest
{
    private static GetSupplierByIdQueryHandler CreateHandler(WmsDbContext db)
    {
        return new GetSupplierByIdQueryHandler(CreateUnitOfWork(db));
    }

    [Fact]
    public async Task Handle_WithActiveSupplier_ReturnsDetails()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var supplier = Supplier.Create(TenantA, "SUP01", "Supplier Name", "Contact", "123", "sup@mail.com", "Address");
        db.Set<Supplier>().Add(supplier);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var query = new GetSupplierByIdQuery(TenantA, supplier.Id);

        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        result.Id.Should().Be(supplier.Id);
        result.Code.Should().Be("SUP01");
        result.Name.Should().Be("Supplier Name");
        result.Contact.Should().Be("Contact");
        result.Phone.Should().Be("123");
        result.Email.Should().Be("sup@mail.com");
        result.Address.Should().Be("Address");
        result.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithDeletedSupplier_ReturnsDetailsWithIsDeletedTrue()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var supplier = Supplier.Create(TenantA, "SUP01", "Supplier Name");
        supplier.Delete("admin@mail.com");
        db.Set<Supplier>().Add(supplier);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var query = new GetSupplierByIdQuery(TenantA, supplier.Id);

        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        result.Id.Should().Be(supplier.Id);
        result.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenSupplierNotFound_ThrowsNotFoundException()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var query = new GetSupplierByIdQuery(TenantA, Guid.NewGuid());

        var act = () => handler.Handle(query, TestContext.Current.CancellationToken);
        await act.Should().ThrowAsync<AppException>().Where(x => x.StatusCode == 404 && x.Code == "SUPPLIER_NOT_FOUND");
    }
}
