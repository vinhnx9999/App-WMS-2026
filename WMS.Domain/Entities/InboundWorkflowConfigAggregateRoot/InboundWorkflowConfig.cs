using WMS.Domain.Common;
using WMS.Domain.Enums;
using WMS.Domain.Interfaces;

namespace WMS.Domain.Entities.InboundWorkflowConfigAggregateRoot;

public class InboundWorkflowConfig : BaseEntity, IAggregateRoot
{
    private readonly List<InboundWorkflowStep> _steps = [];

    private InboundWorkflowConfig() { }

    public InboundWorkflowConfig(
        Guid tenantId,
        Guid? warehouseId,
        Guid? supplierId,
        Guid? categoryId,
        bool allowOverReceive = true,
        decimal? overReceiveTolerancePercentage = null)
    {
        TenantId = tenantId;
        WarehouseId = warehouseId;
        SupplierId = supplierId;
        CategoryId = categoryId;
        AllowOverReceive = allowOverReceive;
        OverReceiveTolerancePercentage = overReceiveTolerancePercentage;
    }

    public Guid? WarehouseId { get; private set; }
    public Guid? SupplierId { get; private set; }
    public Guid? CategoryId { get; private set; }
    /// <summary>
    /// Indicates whether over-receiving is allowed for inbound shipments.
    /// If set to true, it means that the system permits receiving quantities beyond the ordered amount.
    /// If set to false, it means that over-receiving is not allowed, and any attempt to receive more than the ordered quantity will result in an exception or alert.
    /// </summary>
    public bool AllowOverReceive { get; private set; } = true;
    /// <summary>
    /// The percentage of over-receive tolerance allowed for inbound shipments. 
    /// This value is used to determine the maximum quantity that can be received beyond the ordered quantity without triggering an exception or alert.
    /// If set to null, it indicates that there is no specific tolerance percentage defined.
    /// </summary>
    public decimal? OverReceiveTolerancePercentage { get; private set; }

    public IReadOnlyCollection<InboundWorkflowStep> Steps => _steps.AsReadOnly();

    public void UpdateSettings(bool allowOverReceive, decimal? overReceiveTolerancePercentage)
    {
        AllowOverReceive = allowOverReceive;
        OverReceiveTolerancePercentage = overReceiveTolerancePercentage;
    }

    public void UpdateSteps(IEnumerable<InboundWorkflowStep> newSteps)
    {
        var stepsList = newSteps?.ToList() ?? throw new DomainException("Steps collection cannot be null.");

        if (stepsList.Count == 0)
        {
            throw new DomainException("Workflow steps cannot be empty.");
        }

        var hasPutaway = stepsList.Any(s => s.StepType == InboundStepType.Putaway);
        if (!hasPutaway)
        {
            throw new DomainException("Putaway step is mandatory in the workflow.");
        }

        var highestSequenceStep = stepsList.OrderByDescending(s => s.Sequence).First();
        if (highestSequenceStep.StepType != InboundStepType.Putaway)
        {
            throw new DomainException("Putaway step must be the final step in the sequence.");
        }

        _steps.Clear();
        foreach (var step in stepsList)
        {
            _steps.Add(step);
        }
    }
}
