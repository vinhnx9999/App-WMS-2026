using FluentAssertions;
using Microsoft.EntityFrameworkCore.Metadata;
using WMS.Domain.Entities;
using ProductAggregate = WMS.Domain.Entities.Product.Product;

namespace DP.AppWMS.Tests.Skus;

public sealed class SkuEfModelTests : BaseSkuHandlerTest
{
    [Fact]
    public async Task Model_ShouldKeepProductIdAsScalarWithoutProductRelationship()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var skuType = db.Model.FindEntityType(typeof(Sku));

        skuType.Should().NotBeNull();
        skuType!.FindProperty(nameof(Sku.ProductId)).Should().NotBeNull();
        skuType.GetNavigations().Should().NotContain(x => x.TargetEntityType.ClrType == typeof(ProductAggregate));
        skuType.GetForeignKeys().Should().NotContain(x => x.PrincipalEntityType.ClrType == typeof(ProductAggregate));
    }
}
