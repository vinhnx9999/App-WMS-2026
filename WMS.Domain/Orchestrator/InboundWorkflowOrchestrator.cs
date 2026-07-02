using WMS.Domain.Entities.InboundWorkflowConfigAggregateRoot;
using WMS.Domain.Enums;

namespace WMS.Domain.Orchestrator;

public class InboundWorkflowOrchestrator
{
    public InboundWorkflowConfig ResolveConfig(
        Guid warehouseId,
        Guid? supplierId,
        Guid? categoryId,
        IEnumerable<InboundWorkflowConfig> configs)
    {
        var bestMatch = configs
              .Select(c => new { Config = c, Priority = GetMatchPriority(c, warehouseId, supplierId, categoryId) })
              .Where(x => x.Priority > 0)
              .MaxBy(x => x.Priority)?
              .Config;


        return bestMatch ?? CreateDefaultFallback();
    }

    private int GetMatchPriority(InboundWorkflowConfig c, Guid warehouseId, Guid? supplierId, Guid? categoryId)
    {
        // 1. Exact match: Warehouse + Supplier + Category
        if (c.WarehouseId == warehouseId && c.SupplierId == supplierId && c.CategoryId == categoryId && c.SupplierId != null && c.CategoryId != null)
            return 5;

        // 2. Match: Warehouse + Supplier
        if (c.WarehouseId == warehouseId && c.SupplierId == supplierId && c.SupplierId != null && c.CategoryId == null)
            return 4;

        // 3. Match: Warehouse + Category
        if (c.WarehouseId == warehouseId && c.CategoryId == categoryId && c.SupplierId == null && c.CategoryId != null)
            return 3;

        // 4. Match: Warehouse only
        if (c.WarehouseId == warehouseId && c.SupplierId == null && c.CategoryId == null)
            return 2;

        // 5. Global Default
        if (c.WarehouseId == null && c.SupplierId == null && c.CategoryId == null)
            return 1;

        return 0;
    }

    private InboundWorkflowConfig CreateDefaultFallback()
    {
        var config = new InboundWorkflowConfig(Guid.Empty, null, null, null, true, null);
        config.UpdateSteps(new List<InboundStepDefinition>
        {
            new(InboundStepType.PO, 0, "Plan/PO"),
            new(InboundStepType.Receive, 1, "Receive"),
            new(InboundStepType.QC, 2, "Quality Control"),
            new(InboundStepType.Putaway, 3, "Putaway")
        });
        return config;
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
