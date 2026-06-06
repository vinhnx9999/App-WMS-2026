using FluentAssertions;
using WMS.Domain.Entities.Product;

namespace DP.AppWMS.Tests.Products;

public sealed class ProductUpdateSkuTests
{
    [Theory]
    [InlineData("AddSku")]
    [InlineData("UpdateSku")]
    [InlineData("DeleteSku")]
    public void Product_ShouldNotExposeSkuLifecycleMethods(string methodName)
    {
        typeof(Product).GetMethod(methodName).Should().BeNull();
    }
}
