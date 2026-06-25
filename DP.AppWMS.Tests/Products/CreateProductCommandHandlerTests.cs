using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using WMS.Application.Common.Models;
using WMS.Application.Common.Service;
using WMS.Application.Products.Commands.CreateProduct;
using WMS.Domain.Entities.ProductAggregateRoot;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;

namespace DP.AppWMS.Tests.Products;

public sealed class CreateProductCommandHandlerTests : BaseProductHandlerTest
{
    private readonly Mock<ISequenceCodeGenerator> _sequenceCodeGeneratorMock = new();

    [Fact]
    public async Task Handle_ValidRequestWithManualCode_CreatesProduct()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var command = new CreateProductCommand(TenantA, "  Prd-Man-001  ", "Test Product", "Test Desc", null);

        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        result.Id.Should().NotBeEmpty();
        result.TenantId.Should().Be(TenantA);
        result.ProductCode.Should().Be("PRD-MAN-001"); // Uppercase and normalized
        result.ProductName.Should().Be("Test Product");
        result.Description.Should().Be("Test Desc");
        result.CategoryId.Should().BeNull();

        var savedProduct = await db.Products.FirstOrDefaultAsync(x => x.Id == result.Id, TestContext.Current.CancellationToken);
        savedProduct.Should().NotBeNull();
        savedProduct!.ProductCode.Should().Be("PRD-MAN-001");
        savedProduct.ProductName.Should().Be("Test Product");
    }

    [Fact]
    public async Task Handle_ValidRequestWithEmptyCode_GeneratesSequenceCode()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        _sequenceCodeGeneratorMock
            .Setup(x => x.NextAsync(TenantA, CodeSequenceTypes.Product, It.IsAny<CancellationToken>()))
            .ReturnsAsync("PRD-000001");

        var handler = CreateHandler(db);
        var command = new CreateProductCommand(TenantA, "", "Test Product", null, null);

        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        result.ProductCode.Should().Be("PRD-000001");
        _sequenceCodeGeneratorMock.Verify(x => x.NextAsync(TenantA, CodeSequenceTypes.Product, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateManualCode_ThrowsConflict()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        // Seed existing product
        db.Products.Add(Product.Create(TenantA, "PRD-DUP", "Existing Product"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var command = new CreateProductCommand(TenantA, "prd-dup", "New Product Name", null, null);

        var act = () => handler.Handle(command, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AppException>()
            .Where(x => x.StatusCode == 409 && x.Code == "DUPLICATE_PRODUCT" && x.Message == "Product code already exists for this tenant.");
    }

    [Fact]
    public async Task Handle_EmptyProductName_ThrowsValidationFailed()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var command = new CreateProductCommand(TenantA, "PRD-NEW", "  ", null, null);

        var act = () => handler.Handle(command, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AppException>()
            .Where(x => x.StatusCode == 400 && x.Code == "VALIDATION_FAILED" && x.Message == "Product name is required.");
    }

    [Fact]
    public async Task Handle_ValidCategoryId_CreatesProduct()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var category = await AddTestCategory(db, TenantA, "Electronics", TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var command = new CreateProductCommand(TenantA, "PRD-CAT", "Product with Category", null, category.Id);

        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        result.CategoryId.Should().Be(category.Id);
    }

    [Fact]
    public async Task Handle_InvalidCategoryId_ThrowsNotFound()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var command = new CreateProductCommand(TenantA, "PRD-CAT", "Product with Category", null, Guid.NewGuid());

        var act = () => handler.Handle(command, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AppException>()
            .Where(x => x.StatusCode == 404 && x.Code == "CATEGORY_NOT_FOUND" && x.Message == "Category not found.");
    }

    [Fact]
    public async Task Handle_CrossTenantCategoryId_ThrowsNotFound()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var category = await AddTestCategory(db, TenantB, "Electronics", TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var command = new CreateProductCommand(TenantA, "PRD-CAT", "Product with Category", null, category.Id);

        var act = () => handler.Handle(command, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AppException>()
            .Where(x => x.StatusCode == 404 && x.Code == "CATEGORY_NOT_FOUND" && x.Message == "Category not found.");
    }

    private CreateProductCommandHandler CreateHandler(WmsDbContext db)
    {
        return new CreateProductCommandHandler(CreateUnitOfWork(db), _sequenceCodeGeneratorMock.Object);
    }
}
