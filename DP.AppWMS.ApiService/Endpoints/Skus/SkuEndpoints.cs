using MediatR;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Common.Models;
using WMS.Application.Product.Skus.Commands.DeleteSku;
using WMS.Application.Product.Skus.Commands.UpdateSku;
using WMS.Application.Product.Skus.DTOs;
using WMS.Application.Product.Skus.Queries.GetSkuById;
using WMS.Application.Product.Skus.Queries.SearchSkus;
using WMS.Domain.Interfaces;

namespace DP.AppWMS.ApiService.Endpoints.Skus;

public sealed class SkuEndpoints : IEndpoint
{
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

    private async Task<IResult> SearchSkus(
           [FromQuery] string? search,
           [FromQuery] Guid? categoryId,
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
            page,
            limit), cancellationToken);

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
            request.CategoryId,
            request.Name,
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
}
