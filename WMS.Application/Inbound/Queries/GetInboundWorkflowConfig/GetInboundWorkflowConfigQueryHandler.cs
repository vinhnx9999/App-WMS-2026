using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Domain.Entities.InboundWorkflowConfigAggregateRoot;
using WMS.Domain.Interfaces;
using WMS.Domain.Orchestrator;

namespace WMS.Application.Inbound.Queries.GetInboundWorkflowConfig;

public sealed record GetInboundWorkflowConfigQuery(
    Guid TenantId,
    Guid WarehouseId) : IRequest<InboundWorkflowConfigResponse>;

public sealed class GetInboundWorkflowConfigQueryHandler(IUnitOfWork uow)
    : IRequestHandler<GetInboundWorkflowConfigQuery, InboundWorkflowConfigResponse>
{
    private readonly IUnitOfWork _uow = uow;

    public async Task<InboundWorkflowConfigResponse> Handle(GetInboundWorkflowConfigQuery request, CancellationToken ct)
    {
        var configs = await _uow.Repository<InboundWorkflowConfig>().Query()
            .Include(c => c.Steps)
            .Where(c => c.TenantId == request.TenantId && !c.IsDeleted)
            .ToListAsync(ct);

        var orchestrator = new InboundWorkflowOrchestrator();
        var resolvedConfig = orchestrator.ResolveConfig(request.WarehouseId, null, null, configs);

        var stepsDto = resolvedConfig.Steps
            .OrderBy(s => s.Sequence)
            .Select(s => new InboundWorkflowStepResponse(s.StepType, s.Sequence, s.DisplayName))
            .ToList();

        return new InboundWorkflowConfigResponse(
            resolvedConfig.TenantId == Guid.Empty ? null : resolvedConfig.Id,
            resolvedConfig.WarehouseId,
            resolvedConfig.SupplierId,
            resolvedConfig.CategoryId,
            resolvedConfig.AllowOverReceive,
            resolvedConfig.OverReceiveTolerancePercentage,
            stepsDto);
    }
}

