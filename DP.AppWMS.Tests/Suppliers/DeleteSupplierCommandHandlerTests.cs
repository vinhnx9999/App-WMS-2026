using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Suppliers.Commands.DeleteSupplier;
using WMS.Domain.Entities.Master;
using WMS.Infrastructure.Persistence;

namespace DP.AppWMS.Tests.Suppliers;

public sealed class DeleteSupplierCommandHandlerTests : BaseSupplierHandlerTest
{
    private static DeleteSupplierCommandHandler CreateHandler(WmsDbContext db)
    {
        return new DeleteSupplierCommandHandler(CreateUnitOfWork(db));
    }

    [Fact]
    public async Task Handle_WithValidId_SoftDeletesSupplier()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var supplier = Supplier.Create(TenantA, "SUP01", "Supplier Name");
        db.Set<Supplier>().Add(supplier);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var command = new DeleteSupplierCommand(TenantA, supplier.Id);

        await handler.Handle(command, TestContext.Current.CancellationToken);

        // Under normal query (excluding deleted), it should not be found
        var queried = await db.Set<Supplier>().FirstOrDefaultAsync(x => x.Id == supplier.Id && !x.IsDeleted, TestContext.Current.CancellationToken);
        queried.Should().BeNull();

        // Bypassing filter, it should exist and have IsDeleted = true
        var raw = await db.Set<Supplier>().IgnoreQueryFilters().FirstAsync(x => x.Id == supplier.Id, TestContext.Current.CancellationToken);
        raw.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenNotFound_ThrowsNotFoundException()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var command = new DeleteSupplierCommand(TenantA, Guid.NewGuid());

        var act = () => handler.Handle(command, TestContext.Current.CancellationToken);
        await act.Should().ThrowAsync<AppException>().Where(x => x.StatusCode == 404 && x.Code == "SUPPLIER_NOT_FOUND");
    }
}
