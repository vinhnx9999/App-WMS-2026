using WMS.Domain.Common;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities.WcsIntegration;

public class WcsSubTask : BaseEntity
{
    public Guid WcsTaskId { get; private set; }
    public string PalletCode { get; private set; } = null!;
    public string FromLocationCode { get; private set; } = "0.0.0";
    public string ToLocationCode { get; private set; } = null!;
    public WcsTaskStatus Status { get; private set; } = WcsTaskStatus.Pending;
    public int Priority { get; private set; } = 0;
    public string? ErrorMessage { get; private set; }

    private WcsSubTask() { }

    public WcsSubTask(
        Guid tenantId,
        Guid wcsTaskId,
        string palletCode,
        string toLocationCode,
        string fromLocationCode = "0.0.0",
        int priority = 0)
    {
        TenantId = tenantId;
        WcsTaskId = wcsTaskId;
        PalletCode = palletCode;
        ToLocationCode = toLocationCode;
        FromLocationCode = fromLocationCode;
        Priority = priority;
        Status = WcsTaskStatus.Pending;
    }

    public void StartProcessing()
    {
        if (Status == WcsTaskStatus.Pending)
        {
            Status = WcsTaskStatus.Processing;
        }
    }

    public void Complete()
    {
        if (Status is not (WcsTaskStatus.Done or WcsTaskStatus.Failed))
        {
            Status = WcsTaskStatus.Done;
        }
    }

    public void Fail(string errorMessage)
    {
        if (Status is not (WcsTaskStatus.Done or WcsTaskStatus.Failed))
        {
            Status = WcsTaskStatus.Failed;
            ErrorMessage = errorMessage;
        }
    }

    public void UpdatePriority(int priority)
    {
        Priority = priority;
    }
}
