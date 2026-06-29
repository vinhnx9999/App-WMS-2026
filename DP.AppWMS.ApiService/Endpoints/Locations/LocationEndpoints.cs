using MediatR;
using WMS.Application.Common.Models;
using WMS.Application.Warehouse.Queries.GetLocationsWithOccupancy;

namespace DP.AppWMS.ApiService.Endpoints.Locations
{
    public sealed class LocationEndpoints : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup(ApiRoutes.Groups.Locations);

            group.MapGet("/occupancy", GetOccupancy)
                .WithName("GetLocationsWithOccupancy").WithTags("Locations").RequireAuthorization()
                .Produces<ApiResponse<List<LocationOccupancyDto>>>(StatusCodes.Status200OK)
                .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized);
        }

        private static async Task<IResult> GetOccupancy(
            Guid warehouseId,
            ISender sender,
            CancellationToken cancellationToken)
        {
            var result = await sender.Send(new GetLocationsWithOccupancyQuery(warehouseId), cancellationToken);
            return Results.Ok(ApiResponse<List<LocationOccupancyDto>>.Ok(result));
        }
    }
}
