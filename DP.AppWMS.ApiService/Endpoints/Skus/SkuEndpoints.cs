using MediatR;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Common.Models;
using WMS.Application.Product.Skus.Commands.CreateSku;
using WMS.Application.Product.Skus.Commands.DeleteSku;
using WMS.Application.Product.Skus.Commands.ImportSkus;
using WMS.Application.Product.Skus.Commands.UpdateSku;
using WMS.Application.Product.Skus.DTOs;
using WMS.Application.Product.Skus.Queries.GetSkuById;
using WMS.Application.Product.Skus.Queries.SearchSkus;
using WMS.Domain.Interfaces;

namespace DP.AppWMS.ApiService.Endpoints.Skus;

public sealed class SkuEndpoints : IEndpoint
{
    #region endpoint definitions

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiRoutes.Groups.Skus);

        group.MapGet("/", SearchSkus)
            .WithName("SearchSkus").WithTags("Products").RequireAuthorization()
            .Produces<ApiResponse<PagedResult<SearchSkusResponse>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest);

        group.MapGet("/{id:guid}", GetSkuById)
            .WithName("GetSkuById").WithTags("Products").RequireAuthorization()
            .Produces<ApiResponse<GetSkuByIdResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateSku)
            .WithName("CreateSku").WithTags("Products").RequireAuthorization()
            .Produces<ApiResponse<CreateSkuResponse>>(StatusCodes.Status201Created)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status409Conflict);

        group.MapPost("/import", ImportSkus)
            .WithName("ImportSkus").WithTags("Products").RequireAuthorization()
            .Accepts<ImportSkusRequest>("application/json")
            .Produces<ApiResponse<ImportSkusResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<ImportSkusResponse>>(StatusCodes.Status400BadRequest);

        group.MapPut("/{id:guid}", UpdateSku)
            .WithName("UpdateSku").WithTags("Products").RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", DeleteSku)
            .WithName("DeleteSku").WithTags("Products").RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);
    }

    #endregion


    #region Handlers
    private async Task<IResult> SearchSkus(
           [FromQuery] string? search,
           [FromQuery] Guid? categoryId,
           [FromQuery] Guid? productID,
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
            productID,
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
        var result = await sender.Send(new CreateSkuCommand(
            currentUser.TenantId,
            request.ProductId,
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

    private async Task<IResult> ImportSkus(
        [FromBody] ImportSkusRequest? request,
        ISender sender,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.BadRequest(ApiResponse<ImportSkusResponse>.Fail("Request body is required"));
        }

        if (request.Rows is null || request.Rows.Count == 0)
        {
            return Results.BadRequest(ApiResponse<ImportSkusResponse>.Fail("Rows cannot be empty"));
        }

        var rows = request.Rows
            .Select(row => new ImportSkuRowInput(
                row.RowNumber,
                row.ProductCode,
                row.SkuCode,
                row.SkuName,
                row.CategoryName,
                row.GoodsNature,
                row.SpecificationCode,
                row.UnitOfMeasureCode,
                row.ConversionFactor))
            .ToList();

        var result = await sender.Send(new ImportSkusCommand(
            currentUser.TenantId,
            rows), cancellationToken);

        var response = ApiResponse<ImportSkusResponse>.Ok(result);
        return result.Errors.Count > 0
            ? Results.BadRequest(response)
            : Results.Ok(response);
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

    #endregion

    public sealed record ImportSkusRequest(
        IReadOnlyList<ImportSkuRowRequest>? Rows);

    public sealed record ImportSkuRowRequest(
        int RowNumber,
        string? ProductCode,
        string? SkuCode,
        string? SkuName,
        string? CategoryName,
        string? GoodsNature,
        string? SpecificationCode,
        string? UnitOfMeasureCode,
        decimal? ConversionFactor);
}
