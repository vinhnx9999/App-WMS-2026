using MediatR;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Common.Models;
using WMS.Application.Product.Skus.DTOs;
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
}
