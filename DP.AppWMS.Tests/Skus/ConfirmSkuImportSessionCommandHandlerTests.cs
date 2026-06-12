using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using WMS.Application.Common.Models;
using WMS.Application.Common.Service;
using WMS.Application.Skus.Commands.ImportSku;
using WMS.Domain.Entities;
using WMS.Domain.Entities.Product;
using WMS.Domain.Enums;

namespace DP.AppWMS.Tests.Skus;

public sealed class ConfirmSkuImportSessionCommandHandlerTests : BaseSkuHandlerTest
{
    [Fact]
    public async Task Handle_WhenAllRowsAreValidAndHaveManualSkuCodes_ShouldConfirmAndCreateSkus()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var product = Product.Create(TenantA, "PROD-001", "Test Product");
        db.Set<Product>().Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var session = SkuImportSession.Create(TenantA, "test.xlsx");
        session.AddRow(1, "PROD-001", product.Id, "SKU-001", "SKU Name 1", "Nature 1", "Desc 1", 100m, true, null, null);
        db.Set<SkuImportSession>().Add(session);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sequenceCodeGenerator = new Mock<ISequenceCodeGenerator>();
        var handler = new ConfirmSkuImportSessionCommandHandler(CreateUnitOfWork(db), sequenceCodeGenerator.Object);

        var command = new ConfirmSkuImportSessionCommand(TenantA, session.Id);
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        result.Status.Should().Be("CONFIRMED");
        result.TotalRows.Should().Be(1);
        result.CreatedCount.Should().Be(1);
        result.CreatedItems.Should().ContainSingle();
        result.CreatedItems[0].SkuCode.Should().Be("SKU-001");

        db.Skus.Should().ContainSingle(x => x.SkuCode == "SKU-001" && x.TenantId == TenantA);
        var updatedSession = db.Set<SkuImportSession>().Include(x => x.Rows).First(x => x.Id == session.Id);
        updatedSession.Status.Should().Be("CONFIRMED");
        updatedSession.Rows.First().CreatedSkuId.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WhenAllRowsAreValidAndHaveGeneratedSkuCodes_ShouldConfirmAndCreateSkus()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var product = Product.Create(TenantA, "PROD-001", "Test Product");
        db.Set<Product>().Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var session = SkuImportSession.Create(TenantA, "test.xlsx");
        session.AddRow(1, "PROD-001", product.Id, null, "SKU Name 1", "Nature 1", "Desc 1", 100m, true, null, null);
        db.Set<SkuImportSession>().Add(session);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sequenceCodeGenerator = new Mock<ISequenceCodeGenerator>();
        sequenceCodeGenerator
            .Setup(x => x.NextAsync(TenantA, CodeSequenceTypes.Sku, It.IsAny<CancellationToken>()))
            .ReturnsAsync("SKU-AUTO-100");

        var handler = new ConfirmSkuImportSessionCommandHandler(CreateUnitOfWork(db), sequenceCodeGenerator.Object);

        var command = new ConfirmSkuImportSessionCommand(TenantA, session.Id);
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        result.Status.Should().Be("CONFIRMED");
        result.CreatedCount.Should().Be(1);
        result.CreatedItems[0].SkuCode.Should().Be("SKU-AUTO-100");

        db.Skus.Should().ContainSingle(x => x.SkuCode == "SKU-AUTO-100");
    }

    [Fact]
    public async Task Handle_WhenSessionHasBothValidAndInvalidRows_ShouldConfirmValidRowsOnly()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var product = Product.Create(TenantA, "PROD-001", "Test Product");
        db.Set<Product>().Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var session = SkuImportSession.Create(TenantA, "test.xlsx");
        // Row 1 is valid
        session.AddRow(1, "PROD-001", product.Id, "SKU-VALID", "SKU Name 1", "Nature 1", "Desc 1", 100m, true, null, null);
        // Row 2 is invalid
        session.AddRow(2, "PROD-002", null, "SKU-INVALID", "SKU Name 2", "Nature 2", "Desc 2", 200m, false, "PRODUCT_NOT_FOUND", "Product not found");
        db.Set<SkuImportSession>().Add(session);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sequenceCodeGenerator = new Mock<ISequenceCodeGenerator>();
        var handler = new ConfirmSkuImportSessionCommandHandler(CreateUnitOfWork(db), sequenceCodeGenerator.Object);

        var command = new ConfirmSkuImportSessionCommand(TenantA, session.Id);
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        result.Status.Should().Be("CONFIRMED");
        result.TotalRows.Should().Be(2);
        result.CreatedCount.Should().Be(1);
        result.CreatedItems.Should().ContainSingle(x => x.SkuCode == "SKU-VALID");

        db.Skus.Should().ContainSingle(x => x.SkuCode == "SKU-VALID");
        db.Skus.Should().NotContain(x => x.SkuCode == "SKU-INVALID");
    }

    [Fact]
    public async Task Handle_WhenSessionHasNoValidRows_ShouldThrowAppException()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var session = SkuImportSession.Create(TenantA, "test.xlsx");
        session.AddRow(1, "PROD-002", null, "SKU-INVALID", "SKU Name", "Nature", "Desc", 100m, false, "PRODUCT_NOT_FOUND", "Product not found");
        db.Set<SkuImportSession>().Add(session);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sequenceCodeGenerator = new Mock<ISequenceCodeGenerator>();
        var handler = new ConfirmSkuImportSessionCommandHandler(CreateUnitOfWork(db), sequenceCodeGenerator.Object);

        var command = new ConfirmSkuImportSessionCommand(TenantA, session.Id);
        var act = () => handler.Handle(command, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AppException>()
            .Where(x => x.StatusCode == 409 && x.Code == "IMPORT_SESSION_HAS_NO_VALID_ROWS");
    }

    [Fact]
    public async Task Handle_WhenSessionNotFound_ShouldThrowAppException()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var sequenceCodeGenerator = new Mock<ISequenceCodeGenerator>();
        var handler = new ConfirmSkuImportSessionCommandHandler(CreateUnitOfWork(db), sequenceCodeGenerator.Object);

        var command = new ConfirmSkuImportSessionCommand(TenantA, Guid.NewGuid());
        var act = () => handler.Handle(command, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AppException>()
            .Where(x => x.StatusCode == 404 && x.Code == "IMPORT_SESSION_NOT_FOUND");
    }

    [Fact]
    public async Task Handle_WhenSessionAlreadyConfirmed_ShouldThrowAppException()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var product = Product.Create(TenantA, "PROD-001", "Test Product");
        db.Set<Product>().Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var session = SkuImportSession.Create(TenantA, "test.xlsx");
        session.AddRow(1, "PROD-001", product.Id, "SKU-001", "SKU Name 1", "Nature 1", "Desc 1", 100m, true, null, null);
        session.MarkConfirmed(DateTime.UtcNow);
        db.Set<SkuImportSession>().Add(session);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sequenceCodeGenerator = new Mock<ISequenceCodeGenerator>();
        var handler = new ConfirmSkuImportSessionCommandHandler(CreateUnitOfWork(db), sequenceCodeGenerator.Object);

        var command = new ConfirmSkuImportSessionCommand(TenantA, session.Id);
        var act = () => handler.Handle(command, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AppException>()
            .Where(x => x.StatusCode == 409 && x.Code == "IMPORT_SESSION_ALREADY_CONFIRMED");
    }

    [Fact]
    public async Task Handle_WhenProductDeletedBeforeConfirm_ShouldThrowAppException()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var product = Product.Create(TenantA, "PROD-001", "Test Product");
        db.Set<Product>().Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var session = SkuImportSession.Create(TenantA, "test.xlsx");
        session.AddRow(1, "PROD-001", product.Id, "SKU-001", "SKU Name 1", "Nature 1", "Desc 1", 100m, true, null, null);
        db.Set<SkuImportSession>().Add(session);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Soft-delete the product between validation and confirmation
        product.Delete();
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sequenceCodeGenerator = new Mock<ISequenceCodeGenerator>();
        var handler = new ConfirmSkuImportSessionCommandHandler(CreateUnitOfWork(db), sequenceCodeGenerator.Object);

        var command = new ConfirmSkuImportSessionCommand(TenantA, session.Id);
        var act = () => handler.Handle(command, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AppException>()
            .Where(x => x.StatusCode == 409 && x.Code == "PRODUCT_NOT_FOUND");
    }

    [Fact]
    public async Task Handle_WhenSkuCodeCreatedBeforeConfirm_ShouldThrowAppException()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var product = Product.Create(TenantA, "PROD-001", "Test Product");
        db.Set<Product>().Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var session = SkuImportSession.Create(TenantA, "test.xlsx");
        session.AddRow(1, "PROD-001", product.Id, "sku-duplicate", "SKU Name 1", "Nature 1", "Desc 1", 100m, true, null, null);
        db.Set<SkuImportSession>().Add(session);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Add a SKU with duplicate code in database between validation and confirmation
        var existingSku = Sku.Create(TenantA, product.Id, "SKU-DUPLICATE", "Existing", null, null, 0m);
        db.Skus.Add(existingSku);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sequenceCodeGenerator = new Mock<ISequenceCodeGenerator>();
        var handler = new ConfirmSkuImportSessionCommandHandler(CreateUnitOfWork(db), sequenceCodeGenerator.Object);

        var command = new ConfirmSkuImportSessionCommand(TenantA, session.Id);
        var act = () => handler.Handle(command, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AppException>()
            .Where(x => x.StatusCode == 409 && x.Code == "DUPLICATE_SKU");
    }
}
