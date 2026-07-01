using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Domain.Common;
using WMS.Domain.Entities.InboundWorkflowConfigAggregateRoot;
using WMS.Domain.Enums;
using WMS.Domain.Interfaces;

namespace WMS.Application.Inbound.Commands.UpdateInboundWorkflowConfig;

public sealed record UpdateInboundWorkflowConfigStepDto(
    InboundStepType StepType,
    int Sequence,
    string DisplayName);

public sealed record UpdateInboundWorkflowConfigCommand(
    Guid Id,
    Guid TenantId,
    bool AllowOverReceive,
    decimal? OverReceiveTolerancePercentage,
    List<UpdateInboundWorkflowConfigStepDto> Steps) : IRequest;

public sealed class UpdateInboundWorkflowConfigCommandHandler(IUnitOfWork uow)
    : IRequestHandler<UpdateInboundWorkflowConfigCommand>
{
    private readonly IUnitOfWork _uow = uow;

    public async Task Handle(UpdateInboundWorkflowConfigCommand request, CancellationToken ct)
    {
        // 1. Fetch the existing configuration with steps
        var config = await _uow.Repository<InboundWorkflowConfig>().Query()
            .Include(c => c.Steps)
            .FirstOrDefaultAsync(c => c.Id == request.Id && c.TenantId == request.TenantId && !c.IsDeleted, ct);

        if (config is null)
        {
            throw new NotFoundException("InboundWorkflowConfig", request.Id);
        }

        // 2. Update config settings
        config.UpdateSettings(request.AllowOverReceive, request.OverReceiveTolerancePercentage);

        // 3. Update steps
        if (request.Steps == null || request.Steps.Count == 0)
        {
            throw new AppException(400, "VALIDATION_FAILED", "Workflow steps cannot be empty.");
        }

        var domainSteps = request.Steps
            .Select(s => new InboundWorkflowStep(s.StepType, s.Sequence, s.DisplayName))
            .ToList();

        try
        {
            config.UpdateSteps(domainSteps);
        }
        catch (DomainException ex)
        {
            throw new AppException(400, "VALIDATION_FAILED", ex.Message);
        }

        // 4. Save changes
        await _uow.SaveChangesAsync(ct);
    }
}
