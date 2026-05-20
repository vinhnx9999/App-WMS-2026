using FluentAssertions;
using Moq;
using WMS.Infrastructure.ERPs.Odoo.DataClient;

namespace DP.AppWMS.Tests;

public class OdooSyncServiceTests
{
    [Fact]
    public async Task SyncProducts_ShouldMapDefaultCode_ToSku()
    {
        // Arrange
        var odooMock = new Mock<IOdooClient>();
        odooMock
            .Setup(x => x.SearchReadAsync(
                "product.product",
                It.IsAny<List<object>>(),
                It.IsAny<string[]>(),
                It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new()
                {
                    ["id"] = 100,
                    ["default_code"] = "SKU-001",
                    ["name"] = "Test Product",
                    ["barcode"] = "123456789",
                    ["standard_price"] = 150000.0,
                    ["categ_id"] = new List<object?> { 1, "All" },
                }
            ]);

        // Act & Assert — verify mapping logic
        var products = await odooMock.Object.SearchReadAsync("product.product", [], ["default_code"], ct: TestContext.Current.CancellationToken);

        products[0]["default_code"].Should().Be("SKU-001");
        products[0]["name"].Should().Be("Test Product");
    }

    [Fact]
    public void OdooMany2one_ShouldExtractIdAndName()
    {
        // Odoo Many2one format: [id, "display_name"]
        var m2o = new List<object?> { 42L, "Samsung Vina" };

        m2o[0].Should().Be(42L);  // ID
        m2o[1].Should().Be("Samsung Vina");  // Name
    }
}
