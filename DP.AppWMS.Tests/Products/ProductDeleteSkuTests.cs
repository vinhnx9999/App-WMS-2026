using FluentAssertions;
using WMS.Domain.Entities.ProductAggregateRoot;

namespace DP.AppWMS.Tests.Products;

public sealed class ProductDeleteSkuTests
{
    [Fact]
    public void Product_Delete_ShouldOnlyMarkProductDeleted()
    {
        var product = Product.Create(Guid.NewGuid(), "PROD-001", "Test Product");

        product.Delete("admin@test.com");

        product.IsDeleted.Should().BeTrue();
        product.DeletedBy.Should().Be("admin@test.com");
    }
}
