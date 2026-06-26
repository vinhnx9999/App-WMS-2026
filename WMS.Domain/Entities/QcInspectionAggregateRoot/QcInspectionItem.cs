using WMS.Domain.Common;

namespace WMS.Domain.Entities.QcInspectionAggregateRoot;

public class QcInspectionItem : BaseEntity
{
    public Guid SkuId { get; private set; }
    public int ReceivedQuantity { get; private set; }
    public int PassedQuantity { get; private set; }
    public int FailedQuantity { get; private set; }
    public string? Notes { get; private set; }

    public QcInspectionItem(Guid skuId, int receivedQuantity, int passedQuantity, int failedQuantity, string? notes = null)
    {
        if (passedQuantity + failedQuantity != receivedQuantity)
        {
            throw new DomainException("Passed and failed quantities must sum up to the received quantity.");
        }

        SkuId = skuId;
        ReceivedQuantity = receivedQuantity;
        PassedQuantity = passedQuantity;
        FailedQuantity = failedQuantity;
        Notes = notes;
    }

    public void UpdateResults(int passedQuantity, int failedQuantity, string? notes)
    {
        if (passedQuantity + failedQuantity != ReceivedQuantity)
        {
            throw new DomainException("Passed and failed quantities must sum up to the received quantity.");
        }

        PassedQuantity = passedQuantity;
        FailedQuantity = failedQuantity;
        Notes = notes;
    }
}
