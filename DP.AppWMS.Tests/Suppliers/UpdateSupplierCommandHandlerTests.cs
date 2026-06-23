using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Suppliers.Commands.UpdateSupplier;
using WMS.Application.Suppliers.Validators;
using WMS.Domain.Entities.Master;
using WMS.Infrastructure.Persistence;

namespace DP.AppWMS.Tests.Suppliers;

public sealed class UpdateSupplierCommandHandlerTests : BaseSupplierHandlerTest
{
    private static UpdateSupplierCommandHandler CreateHandler(WmsDbContext db)
    {
        return new UpdateSupplierCommandHandler(CreateUnitOfWork(db));
    }

    [Fact]
    public async Task Handle_WithValidData_UpdatesSupplier()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var supplier = Supplier.Create(TenantA, "SUP01", "Old Name", "Old Contact", "123", "old@mail.com", "Old Address");
        db.Set<Supplier>().Add(supplier);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var command = new UpdateSupplierCommand(
            TenantA,
            supplier.Id,
            "New Name",
            "New Contact",
            "456",
            "new@mail.com",
            "New Address");

        await handler.Handle(command, TestContext.Current.CancellationToken);

        var updated = await db.Set<Supplier>().FirstAsync(x => x.Id == supplier.Id, TestContext.Current.CancellationToken);
        updated.Name.Should().Be("New Name");
        updated.Contact.Should().Be("New Contact");
        updated.Phone.Should().Be("456");
        updated.Email.Should().Be("new@mail.com");
        updated.Address.Should().Be("New Address");
        updated.Code.Should().Be("SUP01"); // Code remains unchanged!
    }

    [Fact]
    public async Task Handle_WhenSupplierNotFound_ThrowsNotFoundException()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var command = new UpdateSupplierCommand(TenantA, Guid.NewGuid(), "Name", null, null, null, null);

        var act = () => handler.Handle(command, TestContext.Current.CancellationToken);
        await act.Should().ThrowAsync<AppException>().Where(x => x.StatusCode == 404 && x.Code == "SUPPLIER_NOT_FOUND");
    }

    [Fact]
    public async Task Handle_WhenSupplierDeleted_ThrowsNotFoundException()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var supplier = Supplier.Create(TenantA, "SUP01", "Old Name");
        supplier.Delete("admin@mail.com");
        db.Set<Supplier>().Add(supplier);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var command = new UpdateSupplierCommand(TenantA, supplier.Id, "New Name", null, null, null, null);

        var act = () => handler.Handle(command, TestContext.Current.CancellationToken);
        await act.Should().ThrowAsync<AppException>().Where(x => x.StatusCode == 404 && x.Code == "SUPPLIER_NOT_FOUND");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validator_WhenNameEmpty_ShouldFail(string? emptyVal)
    {
        var validator = new UpdateSupplierCommandValidator();
        var result = validator.Validate(new UpdateSupplierCommand(TenantA, Guid.NewGuid(), emptyVal!, null, null, null, null));
        result.IsValid.Should().BeFalse();
    }
}
