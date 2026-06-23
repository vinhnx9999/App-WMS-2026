using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Suppliers.Commands.CreateSupplier;
using WMS.Application.Suppliers.Validators;
using WMS.Domain.Entities.Master;
using WMS.Infrastructure.Persistence;

namespace DP.AppWMS.Tests.Suppliers;

public sealed class CreateSupplierCommandHandlerTests : BaseSupplierHandlerTest
{
    private static CreateSupplierCommandHandler CreateHandler(WmsDbContext db)
    {
        return new CreateSupplierCommandHandler(CreateUnitOfWork(db));
    }

    [Fact]
    public async Task Handle_WithValidData_CreatesSupplier()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var command = new CreateSupplierCommand(
            TenantA,
            "SUP001",
            "Supplier One",
            "John Doe",
            "123456789",
            "john@supplier.com",
            "123 Main St");

        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        result.Id.Should().NotBeEmpty();
        result.Code.Should().Be("SUP001");
        result.Name.Should().Be("Supplier One");
        result.Contact.Should().Be("John Doe");
        result.Phone.Should().Be("123456789");
        result.Email.Should().Be("john@supplier.com");
        result.Address.Should().Be("123 Main St");

        var saved = await db.Set<Supplier>().FirstOrDefaultAsync(x => x.Id == result.Id, TestContext.Current.CancellationToken);
        saved.Should().NotBeNull();
        saved!.Code.Should().Be("SUP001");
        saved.Name.Should().Be("Supplier One");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validator_WhenCodeOrNameEmpty_ShouldFail(string? emptyVal)
    {
        var validator = new CreateSupplierCommandValidator();

        var badCodeResult = validator.Validate(new CreateSupplierCommand(TenantA, emptyVal!, "Name", null, null, null, null));
        badCodeResult.IsValid.Should().BeFalse();

        var badNameResult = validator.Validate(new CreateSupplierCommand(TenantA, "SUP01", emptyVal!, null, null, null, null));
        badNameResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_WhenFieldsExceedLength_ShouldFail()
    {
        var validator = new CreateSupplierCommandValidator();

        var result = validator.Validate(new CreateSupplierCommand(
            TenantA,
            new string('A', 51),
            new string('B', 256),
            new string('C', 256),
            new string('D', 21),
            "invalid-email",
            new string('E', 501)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Code");
        result.Errors.Should().Contain(x => x.PropertyName == "Name");
        result.Errors.Should().Contain(x => x.PropertyName == "Contact");
        result.Errors.Should().Contain(x => x.PropertyName == "Phone");
        result.Errors.Should().Contain(x => x.PropertyName == "Email");
        result.Errors.Should().Contain(x => x.PropertyName == "Address");
    }
}
