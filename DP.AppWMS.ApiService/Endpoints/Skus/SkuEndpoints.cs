using MediatR;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Common.Models;
using WMS.Application.Skus.Commands.CreateSku;
using WMS.Application.Skus.Commands.DeleteSku;
using WMS.Application.Skus.Commands.ImportSku;
using WMS.Application.Skus.Commands.UpdateSku;
using WMS.Application.Skus.DTOs;
using WMS.Application.Skus.Queries.GetSkuById;
using WMS.Application.Skus.Queries.GetSkuImportSession;
using WMS.Application.Skus.Queries.SearchSkuImportSessions;
using WMS.Application.Skus.Queries.SearchSkus;
using WMS.Domain.Interfaces;

namespace DP.AppWMS.ApiService.Endpoints.Skus;

public sealed class SkuEndpoints : IEndpoint
{
    #region endpoint definitions

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiRoutes.Groups.Skus);

        group.MapGet("/", SearchSkus)
            .WithName("SearchSkus").WithTags("Skus").RequireAuthorization()
            .Produces<ApiResponse<PagedResult<SearchSkusResponse>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest);

        group.MapGet("/{id:guid}", GetSkuById)
            .WithName("GetSkuById").WithTags("Skus").RequireAuthorization()
            .Produces<ApiResponse<GetSkuByIdResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateSku)
            .WithName("CreateSku").WithTags("Skus").RequireAuthorization()
            .Produces<ApiResponse<CreateSkuResponse>>(StatusCodes.Status201Created)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status409Conflict);

        group.MapPost("/import/session", CreateImportSession)
            .WithName("CreateImportSession").WithTags("Skus").RequireAuthorization()
            .Produces<ApiResponse<CreateSkuImportSessionResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest);

        group.MapPost("/import/session/{id:guid}/confirm", ConfirmImportSession)
            .WithName("ConfirmImportSession").WithTags("Skus").RequireAuthorization()
            .Produces<ApiResponse<ConfirmSkuImportSessionResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status409Conflict);

        group.MapGet("/import/sessions", SearchImportSessions)
            .WithName("SearchImportSessions").WithTags("Skus").RequireAuthorization()
            .Produces<ApiResponse<PagedResult<SearchSkuImportSessionsResponse>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest);

        group.MapGet("/import/sessions/{id:guid}", GetImportSessionById)
            .WithName("GetImportSessionById").WithTags("Skus").RequireAuthorization()
            .Produces<ApiResponse<GetSkuImportSessionResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapPost("/import/session/{id:guid}/cancel", CancelImportSession)
            .WithName("CancelImportSession").WithTags("Skus").RequireAuthorization()
            .Produces<ApiResponse<CancelSkuImportSessionResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapPut("/import/session/{id:guid}/rows/{rowId:guid}", UpdateImportRow)
            .WithName("UpdateImportRow").WithTags("Skus").RequireAuthorization()
            .Produces<ApiResponse<UpdateSkuImportRowResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}", UpdateSku)
            .WithName("UpdateSku").WithTags("Skus").RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", DeleteSku)
            .WithName("DeleteSku").WithTags("Skus").RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);
    }

    #endregion


    #region Handlers
    private async Task<IResult> SearchSkus(
           [FromQuery] string? search,
           [FromQuery] Guid? categoryId,
           [FromQuery] Guid? productId,
           [FromQuery] int page,
           [FromQuery] int limit,
           ISender sender,
           ICurrentUser currentUser,
           CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SearchSkusQuery(
            currentUser.TenantId,
            search,
            categoryId,
            productId,
            page,
            limit
           ), cancellationToken);

        return Results.Ok(ApiResponse<PagedResult<SearchSkusResponse>>.Ok(result));
    }

    private async Task<IResult> GetSkuById(
        Guid id,
        ISender sender,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetSkuByIdQuery(currentUser.TenantId, id), cancellationToken);

        return Results.Ok(ApiResponse<GetSkuByIdResponse>.Ok(result));
    }

    private async Task<IResult> CreateSku(
        [FromBody] CreateSkuRequest request,
        ISender sender,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (request.ProductId is null || request.ProductId == Guid.Empty)
        {
            return Results.BadRequest(ApiResponse<CreateSkuResponse>.Fail("ProductId is required."));
        }

        var result = await sender.Send(new CreateSkuCommand(
            currentUser.TenantId,
            request.ProductId.Value,
            request.SkuCode,
            request.Name,
            request.GoodsNature,
            request.Description,
            request.Price), cancellationToken);

        return Results.CreatedAtRoute(
            "GetSkuById",
            new { id = result.Id },
            ApiResponse<CreateSkuResponse>.Ok(result));
    }

    private async Task<IResult> CreateImportSession(
        [FromBody] CreateSkuImportSessionRequest request,
        ISender sender,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateSkuImportSessionCommand(
            currentUser.TenantId,
            request.SourceFileName,
            request.Rows ?? []
        ), cancellationToken);

        return Results.Ok(ApiResponse<CreateSkuImportSessionResponse>.Ok(result));
    }

    private async Task<IResult> ConfirmImportSession(
        Guid id,
        ISender sender,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ConfirmSkuImportSessionCommand(
            currentUser.TenantId,
            id
        ), cancellationToken);

        return Results.Ok(ApiResponse<ConfirmSkuImportSessionResponse>.Ok(result));
    }

    private async Task<IResult> SearchImportSessions(
        [FromQuery] string? status,
        [FromQuery] int page,
        [FromQuery] int limit,
        ISender sender,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SearchSkuImportSessionsQuery(
            currentUser.TenantId,
            status,
            page,
            limit
        ), cancellationToken);

        return Results.Ok(ApiResponse<PagedResult<SearchSkuImportSessionsResponse>>.Ok(result));
    }

    private async Task<IResult> GetImportSessionById(
        Guid id,
        ISender sender,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetSkuImportSessionQuery(
            currentUser.TenantId,
            id
        ), cancellationToken);

        return Results.Ok(ApiResponse<GetSkuImportSessionResponse>.Ok(result));
    }

    private async Task<IResult> CancelImportSession(
        Guid id,
        ISender sender,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CancelSkuImportSessionCommand(
            currentUser.TenantId,
            id
        ), cancellationToken);

        return Results.Ok(ApiResponse<CancelSkuImportSessionResponse>.Ok(result));
    }

    private async Task<IResult> UpdateSku(
        Guid id,
        [FromBody] UpdateSkuRequest request,
        ISender sender,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateSkuCommand(
            currentUser.TenantId,
            id,
            request.Name,
            request.GoodsNature,
            request.Description,
            request.Price), cancellationToken);

        return Results.NoContent();
    }

    private async Task<IResult> DeleteSku(
        Guid id,
        ISender sender,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteSkuCommand(currentUser.TenantId, id), cancellationToken);

        return Results.NoContent();
    }

    private async Task<IResult> UpdateImportRow(
        Guid id,
        Guid rowId,
        [FromBody] UpdateSkuImportRowRequest request,
        ISender sender,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateSkuImportRowCommand(
            currentUser.TenantId,
            id,
            rowId,
            request.ProductCode,
            request.SkuCode,
            request.Name,
            request.GoodsNature,
            request.Description,
            request.ReferencePrice
        ), cancellationToken);

        return Results.Ok(ApiResponse<UpdateSkuImportRowResponse>.Ok(result));
    }

    #endregion

    public sealed record CreateSkuImportSessionRequest(
        string? SourceFileName,
        IReadOnlyList<ImportSkuRowRequest> Rows);

    public sealed record UpdateSkuImportRowRequest(
        string? ProductCode,
        string? SkuCode,
        string? Name,
        string? GoodsNature,
        string? Description,
        decimal? ReferencePrice);
}
