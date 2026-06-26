using WMS.Domain.Entities.InboundWorkflowConfigAggregateRoot;
using WMS.Domain.Enums;

namespace WMS.Domain.Orchestrator;

public class InboundWorkflowOrchestrator
{
    public InboundWorkflowConfig ResolveConfig(
        Guid warehouseId,
        Guid? supplierId,
        Guid? productCategoryId,
        IEnumerable<InboundWorkflowConfig> configs)
    {
        var configsList = configs.ToList();

        // 1. Exact match on WarehouseId AND SupplierId AND ProductCategoryId
        var match1 = configsList.FirstOrDefault(c =>
            c.WarehouseId == warehouseId &&
            c.SupplierId == supplierId &&
            c.ProductCategoryId == productCategoryId &&
            c.WarehouseId != null &&
            c.SupplierId != null &&
            c.ProductCategoryId != null);
        if (match1 != null) return match1;

        // 2. Match on WarehouseId AND SupplierId
        var match2 = configsList.FirstOrDefault(c =>
            c.WarehouseId == warehouseId &&
            c.SupplierId == supplierId &&
            c.WarehouseId != null &&
            c.SupplierId != null &&
            c.ProductCategoryId == null);
        if (match2 != null) return match2;

        // 3. Match on WarehouseId AND ProductCategoryId
        var match3 = configsList.FirstOrDefault(c =>
            c.WarehouseId == warehouseId &&
            c.ProductCategoryId == productCategoryId &&
            c.WarehouseId != null &&
            c.SupplierId == null &&
            c.ProductCategoryId != null);
        if (match3 != null) return match3;

        // 4. Match on WarehouseId only
        var match4 = configsList.FirstOrDefault(c =>
            c.WarehouseId == warehouseId &&
            c.SupplierId == null &&
            c.ProductCategoryId == null);
        if (match4 != null) return match4;

        // 5. Global Default (all values null)
        var match5 = configsList.FirstOrDefault(c =>
            c.WarehouseId == null &&
            c.SupplierId == null &&
            c.ProductCategoryId == null);
        if (match5 != null) return match5;

        // Fallback default configuration
        var defaultConfig = new InboundWorkflowConfig(Guid.Empty, null, null, null, true, null);
        defaultConfig.UpdateSteps(new List<InboundWorkflowStep>
        {
            new(InboundStepType.PO, 0, "Plan/PO"),
            new(InboundStepType.Receive, 1, "Receive"),
            new(InboundStepType.QC, 2, "Quality Control"),
            new(InboundStepType.Putaway, 3, "Putaway")
        });
        return defaultConfig;
    }

    public InboundStepType? GetNextStep(
        InboundWorkflowConfig config,
        InboundStepType currentStep)
    {
        var steps = config.Steps.OrderBy(s => s.Sequence).ToList();
        var currentStepObj = steps.FirstOrDefault(s => s.StepType == currentStep);
        if (currentStepObj == null) return null;

        var nextStepObj = steps.FirstOrDefault(s => s.Sequence > currentStepObj.Sequence);
        return nextStepObj?.StepType;
    }
}
