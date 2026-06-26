using WMS.Domain.Common;

namespace WMS.Domain.Entities.PutawayTaskAggregateRoot;

public class PutawayTaskItem : BaseEntity
{
    public Guid SkuId { get; private set; }
    public int PutawayQuantity { get; private set; }
    public Guid? TargetLocationId { get; private set; }
    public Guid? ActualLocationId { get; private set; }

    public PutawayTaskItem(Guid skuId, int putawayQuantity, Guid? targetLocationId = null, Guid? actualLocationId = null)
    {
        SkuId = skuId;
        PutawayQuantity = putawayQuantity;
        TargetLocationId = targetLocationId;
        ActualLocationId = actualLocationId;
    }

    public void CompletePutaway(Guid actualLocationId)
    {
        ActualLocationId = actualLocationId;
    }
}
