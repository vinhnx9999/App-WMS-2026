using FluentAssertions;
using WMS.Domain.Common;
using WMS.Domain.Entities.Product;
using DP.AppWMS.Tests.Skus;

namespace DP.AppWMS.Tests.Products;

public sealed class ProductDeleteSkuTests : BaseSkuHandlerTest
{
    [Fact]
    public void DeleteSku_WhenSkuExists_MarksDeleted()
    {
        var product = Product.Create(TenantA, "PROD-001", "Test Product");
        var sku = product.AddSku(TenantA, "SKU-001", "Phone", null, null, 10m);

        product.DeleteSku(sku.Id);

        sku.IsDeleted.Should().BeTrue();
        sku.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public void DeleteSku_WhenSkuDoesNotExist_ThrowsDomainException()
    {
        var product = Product.Create(TenantA, "PROD-001", "Test Product");
        product.AddSku(TenantA, "SKU-001", "Phone", null, null, 10m);

        var act = () => product.DeleteSku(Guid.NewGuid());

        act.Should().Throw<DomainException>()
            .Where(x => x.Code == "SKU_NOT_FOUND");
    }

    [Fact]
    public void DeleteSku_WhenSkuIsAlreadyDeleted_ThrowsDomainException()
    {
        var product = Product.Create(TenantA, "PROD-001", "Test Product");
        var sku = product.AddSku(TenantA, "SKU-001", "Phone", null, null, 10m);
        sku.MarkDeleted();

        var act = () => product.DeleteSku(sku.Id);

        act.Should().Throw<DomainException>()
            .Where(x => x.Code == "SKU_NOT_FOUND");
    }

    [Fact]
    public void DeleteSku_WhenMultipleSkus_DeletesOnlyTarget()
    {
        var product = Product.Create(TenantA, "PROD-001", "Test Product");
        var target = product.AddSku(TenantA, "SKU-001", "Target", null, null, 10m);
        var other = product.AddSku(TenantA, "SKU-002", "Other", null, null, 20m);

        product.DeleteSku(target.Id);

        target.IsDeleted.Should().BeTrue();
        other.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void DeleteSku_WithDeletedBy_SetsDeletedBy()
    {
        var product = Product.Create(TenantA, "PROD-001", "Test Product");
        var sku = product.AddSku(TenantA, "SKU-001", "Phone", null, null, 10m);

        product.DeleteSku(sku.Id, "admin@test.com");

        sku.DeletedBy.Should().Be("admin@test.com");
    }
}
