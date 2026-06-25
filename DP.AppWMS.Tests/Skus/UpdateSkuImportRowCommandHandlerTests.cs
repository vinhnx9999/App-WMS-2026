using FluentAssertions;
using WMS.Application.Common.Models;
using WMS.Application.Skus.Commands.ImportSku;
using WMS.Domain.Entities.ProductAggregateRoot;
using WMS.Domain.Entities.SkuAggregateRoot;

namespace DP.AppWMS.Tests.Skus;

public class UpdateSkuImportRowCommandHandlerTests : BaseSkuHandlerTest
{
    [Fact]
    public async Task Handle_WhenValidUpdate_ShouldUpdateRowAndRecalculateCounters()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        // Seed products
        var prod1 = Product.Create(TenantA, "PROD-01", "Product One");
        var prod2 = Product.Create(TenantA, "PROD-02", "Product Two");
        db.Set<Product>().AddRange(prod1, prod2);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Create import session
        var session = SkuImportSession.Create(TenantA, "import.xlsx");
        session.AddRow(
            rowNumber: 1,
            productCode: "PROD-01",
            productId: prod1.Id,
            skuCode: "SKU-01",
            name: "Sku Name 1",
            goodsNature: "Nature",
            description: "Desc",
            referencePrice: 10m,
            isValid: true,
            errorCode: null,
            errorMessage: null
        );

        db.Set<SkuImportSession>().Add(session);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var row = session.Rows.Single();

        // Handler and Command
        var handler = new UpdateSkuImportRowCommandHandler(CreateUnitOfWork(db));
        var command = new UpdateSkuImportRowCommand(
            TenantId: TenantA,
            ImportSessionId: session.Id,
            ImportRowId: row.Id,
            ProductCode: "PROD-02", // change product
            SkuCode: "SKU-02", // change sku code
            Name: "Sku Name 2 Updated",
            GoodsNature: "Nature 2",
            Description: "Desc 2",
            ReferencePrice: 20m
        );

        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.ImportSessionId.Should().Be(session.Id);
        result.TotalRows.Should().Be(1);
        result.ValidRows.Should().Be(1);
        result.InvalidRows.Should().Be(0);
        result.Rows.Should().ContainSingle();

        var updatedRow = result.Rows.Single();
        updatedRow.ProductCode.Should().Be("PROD-02");
        updatedRow.ProductId.Should().Be(prod2.Id);
        updatedRow.SkuCode.Should().Be("SKU-02");
        updatedRow.Name.Should().Be("Sku Name 2 Updated");
        updatedRow.ReferencePrice.Should().Be(20m);
        updatedRow.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenDuplicateSkuCodeIsResolved_ShouldMarkBothRowsValid()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        // Seed product
        var prod = Product.Create(TenantA, "PROD-01", "Product One");
        db.Set<Product>().Add(prod);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Create import session with duplicate SKUs
        var session = SkuImportSession.Create(TenantA, "import.xlsx");
        session.AddRow(
            rowNumber: 1,
            productCode: "PROD-01",
            productId: prod.Id,
            skuCode: "SKU-DUP",
            name: "Sku Name 1",
            goodsNature: "Nature",
            description: "Desc",
            referencePrice: 10m,
            isValid: false,
            errorCode: "DUPLICATE_SKU_IN_IMPORT",
            errorMessage: "SKU code is duplicated in the import rows."
        );
        session.AddRow(
            rowNumber: 2,
            productCode: "PROD-01",
            productId: prod.Id,
            skuCode: "SKU-DUP",
            name: "Sku Name 2",
            goodsNature: "Nature",
            description: "Desc",
            referencePrice: 10m,
            isValid: false,
            errorCode: "DUPLICATE_SKU_IN_IMPORT",
            errorMessage: "SKU code is duplicated in the import rows."
        );

        db.Set<SkuImportSession>().Add(session);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var targetRow = session.Rows.First(x => x.RowNumber == 2);

        // Handler and Command to resolve duplicate by renaming row 2's SKU code
        var handler = new UpdateSkuImportRowCommandHandler(CreateUnitOfWork(db));
        var command = new UpdateSkuImportRowCommand(
            TenantId: TenantA,
            ImportSessionId: session.Id,
            ImportRowId: targetRow.Id,
            ProductCode: "PROD-01",
            SkuCode: "SKU-RESOLVED", // unique SKU code now
            Name: "Sku Name 2",
            GoodsNature: "Nature",
            Description: "Desc",
            ReferencePrice: 10m
        );

        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.ValidRows.Should().Be(2); // Both rows should be valid now!
        result.InvalidRows.Should().Be(0);

        var row1 = result.Rows.First(x => x.RowNumber == 1);
        row1.IsValid.Should().BeTrue();
        row1.ErrorCode.Should().BeNull();

        var row2 = result.Rows.First(x => x.RowNumber == 2);
        row2.IsValid.Should().BeTrue();
        row2.SkuCode.Should().Be("SKU-RESOLVED");
        row2.ErrorCode.Should().BeNull();
    }
}
