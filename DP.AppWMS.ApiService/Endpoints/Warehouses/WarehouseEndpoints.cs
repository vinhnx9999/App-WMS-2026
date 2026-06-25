using MediatR;
using WMS.Application.Common.Models;
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
    }

    private async Task<IResult> LookupWarehouses(
        ISender sender,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new WarehouseLookupQuery(currentUser.TenantId), cancellationToken);
        return Results.Ok(ApiResponse<List<WarehouseLookupResponse>>.Ok(result));
    }
}
