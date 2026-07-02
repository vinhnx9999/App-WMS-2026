using MediatR;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Common.Models;
using WMS.Application.Inbound.Commands.CreateInboundWorkflowConfig;
using WMS.Application.Inbound.Commands.UpdateInboundWorkflowConfig;
using WMS.Application.Inbound.DTOs;
using WMS.Application.Inbound.Queries.GetInboundWorkflowConfig;
using WMS.Application.Warehouse.Queries.WarehouseLookup;
using WMS.Domain.Interfaces;

namespace DP.AppWMS.ApiService.Endpoints.Warehouses;

public sealed class WarehouseEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiRoutes.Groups.Warehouses);

        group.MapGet("/lookup", LookupWarehouses)
            .WithName("LookupWarehouses").WithTags("Warehouses").RequireAuthorization()
            .Produces<ApiResponse<List<WarehouseLookupResponse>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized);

        group.MapGet("/inbound-workflow-config", GetInboundWorkflowConfig)
              .WithName("GetInboundWorkflowConfig").WithTags("Warehouses").RequireAuthorization()
              .Produces<ApiResponse<InboundWorkflowConfigResponse>>(StatusCodes.Status200OK)
              .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
              .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized);

        group.MapPost("/inbound-workflow-config", CreateInboundWorkflowConfig)
            .WithName("CreateInboundWorkflowConfig").WithTags("Warehouses").RequireAuthorization()
            .Produces<ApiResponse<Guid>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized);

        group.MapPut("/inbound-workflow-config/{id:guid}", UpdateInboundWorkflowConfig)
            .WithName("UpdateInboundWorkflowConfig").WithTags("Warehouses").RequireAuthorization()
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);
    }

    private async Task<IResult> LookupWarehouses(
        ISender sender,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new WarehouseLookupQuery(currentUser.TenantId), cancellationToken);
        return Results.Ok(ApiResponse<List<WarehouseLookupResponse>>.Ok(result));
    }

    private async Task<IResult> GetInboundWorkflowConfig(
       Guid warehouseId,
       ISender sender,
       ICurrentUser currentUser,
       CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetInboundWorkflowConfigQuery(currentUser.TenantId, warehouseId), cancellationToken);
        return Results.Ok(ApiResponse<InboundWorkflowConfigResponse>.Ok(result));
    }

    private async Task<IResult> CreateInboundWorkflowConfig(
        [FromBody] CreateInboundWorkflowConfigRequest request,
        ISender sender,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var command = new CreateInboundWorkflowConfigCommand(
            currentUser.TenantId,
            request.WarehouseId,
            request.SupplierId,
            request.CategoryId,
            request.AllowOverReceive,
            request.OverReceiveTolerancePercentage,
            request.Steps);

        var result = await sender.Send(command, cancellationToken);
        return Results.Ok(ApiResponse<Guid>.Ok(result, "Created Inbound Workflow Config Successfully"));
    }

    private async Task<IResult> UpdateInboundWorkflowConfig(
        Guid id,
        [FromBody] UpdateInboundWorkflowConfigRequest request,
        ISender sender,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var command = new UpdateInboundWorkflowConfigCommand(
            id,
            currentUser.TenantId,
            request.AllowOverReceive,
            request.OverReceiveTolerancePercentage,
            request.Steps);

        await sender.Send(command, cancellationToken);
        return Results.Ok(ApiResponse.Ok(null, "Update Inbound Workflow Config Successfully"));
    }
}
