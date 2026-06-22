using FluentAssertions;
using WMS.Domain.Entities.Product;

namespace DP.AppWMS.Tests.Skus;

public class SkuImportSessionDomainTests
{
    [Fact]
    public void SkuImportRow_UpdateValues_ShouldMutateFieldsCorrectly()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var row = SkuImportRow.Create(
            tenantId,
            sessionId,
            rowNumber: 1,
            productCode: "OLD-PROD",
            productId: Guid.NewGuid(),
            skuCode: "OLD-SKU",
            name: "Old Name",
            goodsNature: "Old Nature",
            description: "Old Desc",
            referencePrice: 100m,
            isValid: true,
            errorCode: null,
            errorMessage: null
        );

        // Act - Call the method we are about to implement
        row.UpdateValues(
            productCode: "new-prod",
            skuCode: "new-sku",
            name: "New Name",
            goodsNature: "New Nature",
            description: "New Desc",
            referencePrice: 200m
        );

        // Assert
        row.ProductCode.Should().Be("NEW-PROD"); // should be normalized (trimmed, uppercase)
        row.SkuCode.Should().Be("NEW-SKU");
        row.Name.Should().Be("New Name");
        row.GoodsNature.Should().Be("New Nature");
        row.Description.Should().Be("New Desc");
        row.ReferencePrice.Should().Be(200m);
    }

    [Fact]
    public void SkuImportSession_UpdateRow_ShouldUpdateTargetRowAndAllowRecalculatingCounters()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var session = SkuImportSession.Create(tenantId, "test.xlsx");
        
        session.AddRow(
            rowNumber: 1,
            productCode: "OLD-PROD",
            productId: null,
            skuCode: "OLD-SKU",
            name: "Old Name",
            goodsNature: "Old Nature",
            description: "Old Desc",
            referencePrice: 100m,
            isValid: true,
            errorCode: null,
            errorMessage: null
        );

        var row = session.Rows.Single();
        var rowId = row.Id;

        // Act - Call the methods we are about to implement
        session.UpdateRow(
            rowId,
            productCode: "new-prod",
            skuCode: "new-sku",
            name: "New Name",
            goodsNature: "New Nature",
            description: "New Desc",
            referencePrice: 200m
        );

        // Mark the row as invalid for test purposes to check counter recalculation
        row.MarkInvalid("SOME_ERROR", "Some error occurred");
        session.RecalculateSessionCounters();

        // Assert
        row.ProductCode.Should().Be("NEW-PROD");
        row.SkuCode.Should().Be("NEW-SKU");
        row.Name.Should().Be("New Name");
        row.GoodsNature.Should().Be("New Nature");
        row.Description.Should().Be("New Desc");
        row.ReferencePrice.Should().Be(200m);

        session.TotalRows.Should().Be(1);
        session.ValidRows.Should().Be(0);
        session.InvalidRows.Should().Be(1);
    }
}
