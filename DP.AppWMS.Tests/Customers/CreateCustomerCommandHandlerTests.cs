using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using WMS.Application.Common.Service;
using WMS.Application.Customers.Commands.CreateCustomer;
using WMS.Application.Customers.Validators;
using WMS.Domain.Entities.Master;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;

namespace DP.AppWMS.Tests.Customers;

public sealed class CreateCustomerCommandHandlerTests : BaseCustomerHandlerTest
{
    private readonly Mock<ISequenceCodeGenerator> _sequenceCodeGeneratorMock = new();

    private CreateCustomerCommandHandler CreateHandler(WmsDbContext db)
    {
        return new CreateCustomerCommandHandler(CreateUnitOfWork(db), _sequenceCodeGeneratorMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidData_CreatesCustomer()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var command = new CreateCustomerCommand(
            TenantA,
            "CUST001",
            "Customer One",
            "123 Main St",
            "123456789",
            "Wholesale");

        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        result.Id.Should().NotBeEmpty();
        result.Code.Should().Be("CUST001");
        result.Name.Should().Be("Customer One");
        result.Address.Should().Be("123 Main St");
        result.Phone.Should().Be("123456789");
        result.Type.Should().Be("Wholesale");

        var saved = await db.Set<Customer>().FirstOrDefaultAsync(x => x.Id == result.Id, TestContext.Current.CancellationToken);
        saved.Should().NotBeNull();
        saved!.Code.Should().Be("CUST001");
        saved.Name.Should().Be("Customer One");
    }

    [Fact]
    public async Task Handle_WithEmptyCode_GeneratesCustomerCode()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        _sequenceCodeGeneratorMock
            .Setup(x => x.NextAsync(TenantA, CodeSequenceTypes.Customer, It.IsAny<CancellationToken>()))
            .ReturnsAsync("CUST-000001");

        var handler = CreateHandler(db);
        var command = new CreateCustomerCommand(
            TenantA,
            "",
            "Customer One",
            "123 Main St",
            "123456789",
            "Wholesale");

        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        result.Id.Should().NotBeEmpty();
        result.Code.Should().Be("CUST-000001");
        result.Name.Should().Be("Customer One");

        _sequenceCodeGeneratorMock.Verify(x => x.NextAsync(TenantA, CodeSequenceTypes.Customer, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validator_WhenNameEmpty_ShouldFail(string? emptyVal)
    {
        var validator = new CreateCustomerCommandValidator();

        var badNameResult = validator.Validate(new CreateCustomerCommand(TenantA, "CUST01", emptyVal!, null, null, null));
        badNameResult.IsValid.Should().BeFalse();

        var goodCodeResult = validator.Validate(new CreateCustomerCommand(TenantA, emptyVal, "Name", null, null, null));
        goodCodeResult.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_WhenFieldsExceedLength_ShouldFail()
    {
        var validator = new CreateCustomerCommandValidator();

        var result = validator.Validate(new CreateCustomerCommand(
            TenantA,
            new string('A', 51),
            new string('B', 256),
            new string('C', 256),
            new string('D', 21),
            new string('E', 51)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Code");
        result.Errors.Should().Contain(x => x.PropertyName == "Name");
        result.Errors.Should().Contain(x => x.PropertyName == "Address");
        result.Errors.Should().Contain(x => x.PropertyName == "Phone");
        result.Errors.Should().Contain(x => x.PropertyName == "Type");
    }
}
