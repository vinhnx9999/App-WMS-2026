using DP.AppWMS.Tests.Skus;
using FluentAssertions;
using WMS.Domain.Common;
using WMS.Domain.Entities.Product;

namespace DP.AppWMS.Tests.Products;

public sealed class ProductUpdateSkuTests : BaseSkuHandlerTest
{
    [Fact]
    public void UpdateSku_WhenSkuExists_UpdatesFields()
    {
        var product = Product.Create(TenantA, "PROD-001", "Test Product");
        var sku = product.AddSku(TenantA, "SKU-001", "Phone", "Electronic", "Original", 10m);

        product.UpdateSku(sku.Id, "Tablet", "Gadget", "Updated", 25.5m);

        sku.Name.Should().Be("Tablet");
        sku.GoodsNature.Should().Be("Gadget");
        sku.Description.Should().Be("Updated");
        sku.ReferencePrice.Should().Be(25.5m);
    }

    [Fact]
    public void UpdateSku_WhenSkuDoesNotExist_ThrowsDomainException()
    {
        var product = Product.Create(TenantA, "PROD-001", "Test Product");
        product.AddSku(TenantA, "SKU-001", "Phone", null, null, 10m);

        var act = () => product.UpdateSku(Guid.NewGuid(), "Tablet");

        act.Should().Throw<DomainException>()
            .Where(x => x.Code == "SKU_NOT_FOUND");
    }

    [Fact]
    public void UpdateSku_WhenSkuIsDeleted_ThrowsDomainException()
    {
        var product = Product.Create(TenantA, "PROD-001", "Test Product");
        var sku = product.AddSku(TenantA, "SKU-001", "Phone", null, null, 10m);
        sku.MarkDeleted();

        var act = () => product.UpdateSku(sku.Id, "Tablet");

        act.Should().Throw<DomainException>()
            .Where(x => x.Code == "SKU_NOT_FOUND");
    }

    [Fact]
    public void UpdateSku_WithNegativePrice_ThrowsDomainException()
    {
        var product = Product.Create(TenantA, "PROD-001", "Test Product");
        var sku = product.AddSku(TenantA, "SKU-001", "Phone", null, null, 10m);

        var act = () => product.UpdateSku(sku.Id, referencePrice: -1);

        act.Should().Throw<DomainException>()
            .Where(x => x.Code == "INVALID_REFERENCE_PRICE");
    }

    [Fact]
    public void UpdateSku_WhenAllFieldsNull_KeepsExistingValues()
    {
        var product = Product.Create(TenantA, "PROD-001", "Test Product");
        var sku = product.AddSku(TenantA, "SKU-001", "Phone", "Electronic", "Desc", 10m);

        product.UpdateSku(sku.Id);

        sku.Name.Should().Be("Phone");
        sku.GoodsNature.Should().Be("Electronic");
        sku.Description.Should().Be("Desc");
        sku.ReferencePrice.Should().Be(10m);
    }
}
