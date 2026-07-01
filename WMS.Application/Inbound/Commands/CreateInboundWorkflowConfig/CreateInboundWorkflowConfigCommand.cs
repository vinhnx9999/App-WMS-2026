using MediatR;
using WMS.Domain.Enums;

namespace WMS.Application.Inbound.Commands.CreateInboundWorkflowConfig;

public sealed record CreateInboundWorkflowConfigStepDto(
    InboundStepType StepType,
    int Sequence,
    string DisplayName);

public sealed record CreateInboundWorkflowConfigCommand(
    Guid TenantId,
    Guid WarehouseId,
    Guid? SupplierId,
    Guid? CategoryId,
    bool AllowOverReceive,
    decimal? OverReceiveTolerancePercentage,
    List<CreateInboundWorkflowConfigStepDto> Steps) : IRequest<Guid>;


