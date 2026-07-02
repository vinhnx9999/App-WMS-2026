using WMS.Domain.Common;
using WMS.Domain.Entities.PalletAggregateRoot;

namespace WMS.Domain.Services;

public class PalletPutawayDomainService
{
    public void ValidatePutawayConstraints(
        Pallet pallet,
        Guid targetSkuId,
        int newQuantity,
        int maxQtyInPallet,
        IReadOnlyDictionary<Guid, int> currentQuantitiesOnPallet)
    {
        if (!pallet.IsMixSku)
        {
            foreach (var skuId in currentQuantitiesOnPallet.Keys)
            {
                if (skuId != targetSkuId)
                {
                    throw new DomainException($"Pallet {pallet.PalletCode} does not allow mixing different SKUs.");
                }
            }
        }

        var currentQty = currentQuantitiesOnPallet.TryGetValue(targetSkuId, out var qty) ? qty : 0;
        if (currentQty + newQuantity > maxQtyInPallet)
        {
            throw new DomainException($"Adding quantity exceeds the maximum pallet capacity of {maxQtyInPallet}.");
        }
    }
}
