using WMS.Application.Inbound.Commands.CreateInboundWorkflowConfig;

namespace WMS.Application.Inbound.DTOs;

public sealed record CreateInboundWorkflowConfigRequest(
   Guid WarehouseId,
   Guid? SupplierId,
   Guid? CategoryId,
   bool AllowOverReceive,
   decimal? OverReceiveTolerancePercentage,
   List<CreateInboundWorkflowConfigStepDto> Steps);
