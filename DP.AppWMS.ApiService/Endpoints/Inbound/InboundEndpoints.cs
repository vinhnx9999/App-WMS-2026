using DP.AppWMS.ApiService.Filters;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Common.Models;
using WMS.Application.Inbound.Commands.CompletePutaway;
using WMS.Application.Inbound.Commands.CompleteQc;
using WMS.Application.Inbound.Commands.CompleteReceipt;
using WMS.Application.Inbound.Commands.CreateDirectPutaway;
using WMS.Application.Inbound.Commands.CreateReceipt;
using WMS.Application.Inbound.Commands.StartQc;
using WMS.Application.Inbound.DTOs;
using WMS.Application.Inbound.Services;
using WMS.Domain.Interfaces;

namespace DP.AppWMS.ApiService.Endpoints.Inbound;

public sealed class InboundEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiRoutes.Groups.Inbound);

        group.MapGet("/", GetList)
            .WithName("GetInboundList").WithTags("Inbound").RequireAuthorization()
            .Produces<ApiResponse<List<InboundOrderDto>>>(StatusCodes.Status200OK);

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetInboundById").WithTags("Inbound").RequireAuthorization()
            .Produces<ApiResponse<InboundOrderDto>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapPost("/", Create)
            .WithName("CreateInbound").WithTags("Inbound")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "admin,manager,planner" })
            .Produces<ApiResponse<InboundOrderDto>>(StatusCodes.Status201Created)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest);

        group.MapPut("/{id:guid}/receive", Receive)
            .WithName("ReceiveInbound").WithTags("Inbound")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "admin,manager,keeper" })
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest);

        group.MapPut("/{id:guid}/cancel", Cancel)
            .WithName("CancelInbound").WithTags("Inbound")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "admin,manager" })
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest);

        group.MapPost("/receipts", CreateReceipt)
            .WithName("CreateReceipt").WithTags("Inbound")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "admin,manager,keeper" })
            .Produces<ApiResponse<Guid>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest);

        group.MapPut("/receipts/{id:guid}/complete", CompleteReceipt)
            .WithName("CompleteReceipt").WithTags("Inbound")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "admin,manager,keeper" })
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapPut("/qc/{id:guid}/start", StartQc)
            .WithName("StartQc").WithTags("Inbound")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "admin,manager,keeper" })
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapPut("/qc/{id:guid}/complete", CompleteQc)
            .WithName("CompleteQc").WithTags("Inbound")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "admin,manager,keeper" })
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapPut("/putaway/{id:guid}/complete", CompletePutaway)
            .WithName("CompletePutaway").WithTags("Inbound")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "admin,manager,keeper" })
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapPost("/putaway/direct", CreateDirectPutaway)
            .WithName("CreateDirectPutaway").WithTags("Inbound")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "admin,manager,keeper" })
            .Produces<ApiResponse<Guid>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> GetList(
        IInboundService svc,
        CancellationToken ct)
    {
        var result = await svc.GetListAsync(ct);
        return Results.Ok(ApiResponse<List<InboundOrderDto>>.Ok(result));
    }

    private static async Task<IResult> GetById(
        Guid id,
        IInboundService svc,
        CancellationToken ct)
    {
        var result = await svc.GetByIdAsync(id, ct);
        return Results.Ok(ApiResponse<InboundOrderDto>.Ok(result));
    }

    private static async Task<IResult> Create(
        [FromBody] CreateInboundRequest request,
        [FromServices] IValidator<CreateInboundRequest> validator,
        IInboundService svc,
        CancellationToken ct)
    {
        await ValidationFilter.ValidateAsync(validator, request);
        var result = await svc.CreateAsync(request, ct);
        return Results.CreatedAtRoute("GetInboundById", new { id = result?.Id }, ApiResponse<InboundOrderDto>.Ok(result));
    }

    private static async Task<IResult> Receive(
        Guid id,
        [FromBody] ReceiveInboundRequest request,
        IInboundService svc,
        CancellationToken ct)
    {
        await svc.ReceiveAsync(id, request, ct);
        return Results.Ok(ApiResponse.Ok(null, "Đã ghi nhận nhận hàng"));
    }

    private static async Task<IResult> Cancel(
        Guid id,
        IInboundService svc,
        CancellationToken ct)
    {
        await svc.CancelAsync(id, ct);
        return Results.Ok(ApiResponse.Ok(null, "Đã hủy đơn nhập"));
    }

    private static async Task<IResult> CreateReceipt(
        [FromBody] CreateReceiptRequest request,
        ISender sender,
        ICurrentUser currentUser,
        CancellationToken ct)
    {
        var result = await sender.Send(new CreateReceiptCommand(request, currentUser.TenantId), ct);
        return Results.Ok(ApiResponse<Guid>.Ok(result, "Đã tạo biên bản nhận hàng"));
    }

    private static async Task<IResult> CompleteReceipt(
        Guid id,
        ISender sender,
        CancellationToken ct)
    {
        await sender.Send(new CompleteReceiptCommand(id), ct);
        return Results.Ok(ApiResponse.Ok(null, "Đã hoàn tất nhận hàng và kích hoạt workflow kế tiếp"));
    }

    private static async Task<IResult> StartQc(
        Guid id,
        ISender sender,
        CancellationToken ct)
    {
        await sender.Send(new StartQcCommand(id), ct);
        return Results.Ok(ApiResponse.Ok(null, "Đã bắt đầu kiểm hàng"));
    }

    private static async Task<IResult> CompleteQc(
        Guid id,
        [FromBody] CompleteQcRequest request,
        ISender sender,
        CancellationToken ct)
    {
        await sender.Send(new CompleteQcCommand(id, request), ct);
        return Results.Ok(ApiResponse.Ok(null, "Đã hoàn tất kiểm hàng và kích hoạt workflow kế tiếp"));
    }

    private static async Task<IResult> CompletePutaway(
        Guid id,
        [FromBody] CompletePutawayRequest request,
        ICurrentUser currentUser,
        ISender sender,
        CancellationToken ct)
    {
        await sender.Send(new CompletePutawayCommand(currentUser.TenantId, id, request), ct);
        return Results.Ok(ApiResponse.Ok(null, "Đã hoàn tất cất hàng, cập nhật tồn kho và tạo GRN"));
    }

    private static async Task<IResult> CreateDirectPutaway(
        [FromBody] CreateDirectPutawayRequest request,
        ICurrentUser currentUser,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new CreateDirectPutawayCommand(currentUser.TenantId, request), ct);
        return Results.Ok(ApiResponse<Guid>.Ok(result, "Đã tạo nhiệm vụ cất hàng trực tiếp"));
    }
}
