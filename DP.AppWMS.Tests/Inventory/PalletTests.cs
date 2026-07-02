using FluentAssertions;
using WMS.Domain.Common;
using WMS.Domain.Entities.PalletAggregateRoot;

namespace DP.AppWMS.Tests.Inventory;

public class PalletTests
{
    [Fact]
    public void ValidatePutawayConstraints_WhenMixSkuIsFalseAndPalletContainsDifferentSku_ShouldThrowDomainException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var pallet = Pallet.Create(tenantId, "PL-TEST-001");
        pallet.UpdateMixSkuOption(false);

        var existingSkuId = Guid.NewGuid();
        var newSkuId = Guid.NewGuid();

        var currentQuantities = new Dictionary<Guid, int>
        {
            { existingSkuId, 50 }
        };

        var service = new WMS.Domain.Services.PalletPutawayDomainService();

        // Act
        var act = () => service.ValidatePutawayConstraints(
            pallet,
            newSkuId,
            newQuantity: 10,
            maxQtyInPallet: 100,
            currentQuantitiesOnPallet: currentQuantities);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*does not allow mixing different SKUs*");
    }

    [Fact]
    public void ValidatePutawayConstraints_WhenQuantityExceedsMaxQtyInPallet_ShouldThrowDomainException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var pallet = Pallet.Create(tenantId, "PL-TEST-001");

        var skuId = Guid.NewGuid();
        var currentQuantities = new Dictionary<Guid, int>
        {
            { skuId, 80 }
        };

        var service = new WMS.Domain.Services.PalletPutawayDomainService();

        // Act
        var act = () => service.ValidatePutawayConstraints(
            pallet,
            skuId,
            newQuantity: 30, // 80 + 30 = 110 > 100
            maxQtyInPallet: 100,
            currentQuantitiesOnPallet: currentQuantities);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*exceeds the maximum pallet capacity*");
    }
}
