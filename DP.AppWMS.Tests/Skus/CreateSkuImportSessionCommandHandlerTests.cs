using FluentAssertions;
using WMS.Application.Common.Models;
using WMS.Application.Skus.Commands.ImportSku;
using WMS.Domain.Entities.ProductAggregateRoot;
using WMS.Domain.Entities.SkuAggregateRoot;

namespace DP.AppWMS.Tests.Skus;

public sealed class CreateSkuImportSessionCommandHandlerTests : BaseSkuHandlerTest
{
    [Fact]
    public async Task Handle_WhenRequestIsValid_ShouldCreateSession()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var product = Product.Create(TenantA, "PROD-001", "Test Product");
        db.Set<Product>().Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateSkuImportSessionCommandHandler(CreateUnitOfWork(db));
        var command = new CreateSkuImportSessionCommand(
            TenantId: TenantA,
            SourceFileName: "test.xlsx",
            Rows: new List<ImportSkuRowRequest>
            {
                new(1, "PROD-001", "SKU-001", "SKU Name 1", "Goods Nature 1", "Desc 1", 100m)
            });

        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        result.Status.Should().Be("VALIDATED");
        result.TotalRows.Should().Be(1);
        result.ValidRows.Should().Be(1);
        result.InvalidRows.Should().Be(0);
        result.Rows.Should().ContainSingle();
        result.Rows[0].IsValid.Should().BeTrue();
        result.Rows[0].ProductId.Should().Be(product.Id);

        db.Set<SkuImportSession>().Should().ContainSingle(x => x.Id == result.ImportSessionId);
    }

    [Fact]
    public async Task Handle_WhenNoRowsProvided_ShouldCreateSessionWithEmptyErrorRow()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var handler = new CreateSkuImportSessionCommandHandler(CreateUnitOfWork(db));
        var command = new CreateSkuImportSessionCommand(
            TenantId: TenantA,
            SourceFileName: "empty.xlsx",
            Rows: new List<ImportSkuRowRequest>());

        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        result.Status.Should().Be("VALIDATED");
        result.TotalRows.Should().Be(1);
        result.ValidRows.Should().Be(0);
        result.InvalidRows.Should().Be(1);
        result.Rows.Should().ContainSingle();
        result.Rows[0].IsValid.Should().BeFalse();
        result.Rows[0].ErrorCode.Should().Be("EMPTY_IMPORT");
    }

    [Fact]
    public async Task Handle_WhenDuplicateRowNumbers_ShouldThrowAppException()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var handler = new CreateSkuImportSessionCommandHandler(CreateUnitOfWork(db));
        var command = new CreateSkuImportSessionCommand(
            TenantId: TenantA,
            SourceFileName: "test.xlsx",
            Rows: new List<ImportSkuRowRequest>
            {
                new(1, "PROD-001", "SKU-001", "Name 1", "Nature 1", "Desc 1", 100m),
                new(1, "PROD-002", "SKU-002", "Name 2", "Nature 2", "Desc 2", 200m)
            });

        var act = () => handler.Handle(command, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AppException>()
            .Where(x => x.StatusCode == 400 && x.Code == "DUPLICATE_ROW_NUMBER");
    }

    [Fact]
    public async Task Handle_WhenProductCodeMissing_ShouldMarkRowAsInvalid()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var handler = new CreateSkuImportSessionCommandHandler(CreateUnitOfWork(db));
        var command = new CreateSkuImportSessionCommand(
            TenantId: TenantA,
            SourceFileName: "test.xlsx",
            Rows: new List<ImportSkuRowRequest>
            {
                new(1, null, "SKU-001", "SKU Name", "Nature", "Desc", 100m)
            });

        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        result.InvalidRows.Should().Be(1);
        result.Rows[0].IsValid.Should().BeFalse();
        result.Rows[0].ErrorCode.Should().Be("PRODUCT_CODE_REQUIRED");
    }

    [Fact]
    public async Task Handle_WhenProductDoesNotExist_ShouldMarkRowAsInvalid()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var handler = new CreateSkuImportSessionCommandHandler(CreateUnitOfWork(db));
        var command = new CreateSkuImportSessionCommand(
            TenantId: TenantA,
            SourceFileName: "test.xlsx",
            Rows: new List<ImportSkuRowRequest>
            {
                new(1, "NON_EXISTENT", "SKU-001", "SKU Name", "Nature", "Desc", 100m)
            });

        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        result.InvalidRows.Should().Be(1);
        result.Rows[0].IsValid.Should().BeFalse();
        result.Rows[0].ErrorCode.Should().Be("PRODUCT_NOT_FOUND");
    }

    [Fact]
    public async Task Handle_WhenSkuNameMissing_ShouldMarkRowAsInvalid()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var product = Product.Create(TenantA, "PROD-001", "Test Product");
        db.Set<Product>().Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateSkuImportSessionCommandHandler(CreateUnitOfWork(db));
        var command = new CreateSkuImportSessionCommand(
            TenantId: TenantA,
            SourceFileName: "test.xlsx",
            Rows: new List<ImportSkuRowRequest>
            {
                new(1, "PROD-001", "SKU-001", null, "Nature", "Desc", 100m)
            });

        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        result.InvalidRows.Should().Be(1);
        result.Rows[0].IsValid.Should().BeFalse();
        result.Rows[0].ErrorCode.Should().Be("SKU_NAME_REQUIRED");
    }

    [Fact]
    public async Task Handle_WhenGoodsNatureAndPriceMissing_ShouldMarkRowAsValid()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var product = Product.Create(TenantA, "PROD-001", "Test Product");
        db.Set<Product>().Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateSkuImportSessionCommandHandler(CreateUnitOfWork(db));
        var command = new CreateSkuImportSessionCommand(
            TenantId: TenantA,
            SourceFileName: "test.xlsx",
            Rows: new List<ImportSkuRowRequest>
            {
                new(1, "PROD-001", "SKU-001", "SKU Name", null, "Desc", null)
            });

        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        result.ValidRows.Should().Be(1);
        result.Rows[0].IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenReferencePriceNegative_ShouldMarkRowAsInvalid()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var product = Product.Create(TenantA, "PROD-001", "Test Product");
        db.Set<Product>().Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateSkuImportSessionCommandHandler(CreateUnitOfWork(db));
        var command = new CreateSkuImportSessionCommand(
            TenantId: TenantA,
            SourceFileName: "test.xlsx",
            Rows: new List<ImportSkuRowRequest>
            {
                new(1, "PROD-001", "SKU-001", "SKU Name", "Nature", "Desc", -5m)
            });

        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        result.InvalidRows.Should().Be(1);
        result.Rows[0].IsValid.Should().BeFalse();
        result.Rows[0].ErrorCode.Should().Be("INVALID_REFERENCE_PRICE");
    }

    [Fact]
    public async Task Handle_WhenSkuCodeDuplicatedInRequest_ShouldMarkRowAsInvalid()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var product = Product.Create(TenantA, "PROD-001", "Test Product");
        db.Set<Product>().Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateSkuImportSessionCommandHandler(CreateUnitOfWork(db));
        var command = new CreateSkuImportSessionCommand(
            TenantId: TenantA,
            SourceFileName: "test.xlsx",
            Rows: new List<ImportSkuRowRequest>
            {
                new(1, "PROD-001", "SKU-ABC", "SKU Name 1", null, null, null),
                new(2, "PROD-001", "sku-abc", "SKU Name 2", null, null, null)
            });

        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        result.TotalRows.Should().Be(2);
        result.ValidRows.Should().Be(1);
        result.InvalidRows.Should().Be(1);
        result.Rows[0].IsValid.Should().BeTrue();
        result.Rows[1].IsValid.Should().BeFalse();
        result.Rows[1].ErrorCode.Should().Be("DUPLICATE_SKU_IN_IMPORT");
    }

    [Fact]
    public async Task Handle_WhenSkuCodeAlreadyExists_ShouldMarkRowAsInvalid()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var product = Product.Create(TenantA, "PROD-001", "Test Product");
        db.Set<Product>().Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var existingSku = Sku.Create(TenantA, product.Id, "SKU-ABC", "Existing SKU", null, null, 0m);
        db.Skus.Add(existingSku);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateSkuImportSessionCommandHandler(CreateUnitOfWork(db));
        var command = new CreateSkuImportSessionCommand(
            TenantId: TenantA,
            SourceFileName: "test.xlsx",
            Rows: new List<ImportSkuRowRequest>
            {
                new(1, "PROD-001", "sku-abc", "SKU Name", null, null, null)
            });

        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        result.InvalidRows.Should().Be(1);
        result.Rows[0].IsValid.Should().BeFalse();
        result.Rows[0].ErrorCode.Should().Be("DUPLICATE_SKU");
    }
}
