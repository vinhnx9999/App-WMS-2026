using WMS.Domain.Common;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities.WcsIntegration;

public class WcsSubTaskHistory : BaseEntity
{
    public Guid WcsSubTaskId { get; private set; }
    public WcsTaskStatus FromStatus { get; private set; }
    public WcsTaskStatus ToStatus { get; private set; }
    public string Robot { get; private set; } = null!;
    public string? Notes { get; private set; }

    private WcsSubTaskHistory() { }

    public WcsSubTaskHistory(
        Guid tenantId,
        Guid wcsSubTaskId,
        WcsTaskStatus fromStatus,
        WcsTaskStatus toStatus,
        string robot,
        string? notes = null)
    {
        TenantId = tenantId;
        WcsSubTaskId = wcsSubTaskId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        Robot = robot;
        Notes = notes;
    }
}
