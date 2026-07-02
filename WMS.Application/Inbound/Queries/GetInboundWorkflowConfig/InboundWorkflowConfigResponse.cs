using WMS.Domain.Enums;

namespace WMS.Application.Inbound.Queries.GetInboundWorkflowConfig;

public sealed record InboundWorkflowConfigResponse(
    Guid? Id,
    Guid? WarehouseId,
    Guid? SupplierId,
    Guid? CategoryId,
    bool AllowOverReceive,
    decimal? OverReceiveTolerancePercentage,
    IReadOnlyList<InboundWorkflowStepResponse> Steps);

public sealed record InboundWorkflowStepResponse(
    InboundStepType StepType,
    int Sequence,
    string DisplayName);
