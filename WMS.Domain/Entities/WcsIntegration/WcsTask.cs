using WMS.Domain.Common;
using WMS.Domain.Enums;
using WMS.Domain.Interfaces;

namespace WMS.Domain.Entities.WcsIntegration;

public class WcsTask : BaseEntity, IAggregateRoot
{
    public Guid WarehouseId { get; private set; }
    public Guid WmsPutawayTaskId { get; private set; }
    public string WcsBlockId { get; private set; } = null!;
    public string WcsTaskNumber { get; private set; } = null!;
    public string TaskType { get; private set; } = WcsTaskTypes.Inbound;
    public WcsTaskStatus Status { get; private set; } = WcsTaskStatus.Pending;

    private readonly List<WcsSubTask> _subTasks = new();
    public IReadOnlyCollection<WcsSubTask> SubTasks => _subTasks.AsReadOnly();

    private WcsTask() { }

    public WcsTask(
        Guid tenantId,
        Guid warehouseId,
        Guid wmsPutawayTaskId,
        string wcsBlockId,
        string wcsTaskNumber,
        string taskType = WcsTaskTypes.Inbound)
    {
        TenantId = tenantId;
        WarehouseId = warehouseId;
        WmsPutawayTaskId = wmsPutawayTaskId;
        WcsBlockId = wcsBlockId;
        WcsTaskNumber = wcsTaskNumber;
        TaskType = taskType;
        Status = WcsTaskStatus.Pending;
    }

    public void AddSubTask(WcsSubTask subTask)
    {
        _subTasks.Add(subTask);
        UpdateStatus();
    }

    public void UpdateStatus()
    {
        if (!_subTasks.Any()) return;

        if (_subTasks.All(t => t.Status == WcsTaskStatus.Pending))
        {
            Status = WcsTaskStatus.Pending;
        }
        else if (_subTasks.All(t => t.Status == WcsTaskStatus.Done))
        {
            Status = WcsTaskStatus.Done;
        }
        else if (_subTasks.Any(t => t.Status == WcsTaskStatus.Failed)
                 && !_subTasks.Any(t => t.Status is WcsTaskStatus.Pending or WcsTaskStatus.Processing))
        {
            Status = WcsTaskStatus.Failed;
        }
        else
        {
            Status = WcsTaskStatus.Processing;
        }
    }
}
