using WMS.Application.Inbound.Commands.UpdateInboundWorkflowConfig;

namespace WMS.Application.Inbound.DTOs;

public sealed record UpdateInboundWorkflowConfigRequest(
  bool AllowOverReceive,
  decimal? OverReceiveTolerancePercentage,
  List<UpdateInboundWorkflowConfigStepDto> Steps);
