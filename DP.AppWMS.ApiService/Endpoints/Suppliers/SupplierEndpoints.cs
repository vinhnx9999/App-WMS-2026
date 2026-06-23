using MediatR;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Common.Models;
using WMS.Application.Suppliers.Commands.CreateSupplier;
using WMS.Application.Suppliers.Commands.DeleteSupplier;
using WMS.Application.Suppliers.Commands.RestoreSupplier;
using WMS.Application.Suppliers.Commands.UpdateSupplier;
using WMS.Application.Suppliers.DTOs;
using WMS.Application.Suppliers.Queries.GetSupplierById;
using WMS.Application.Suppliers.Queries.SearchSuppliers;
using WMS.Domain.Interfaces;

namespace DP.AppWMS.ApiService.Endpoints.Suppliers;

public sealed class SupplierEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiRoutes.Groups.Suppliers);

        group.MapPost("/", CreateSupplier)
            .WithName("CreateSupplier").WithTags("Suppliers").RequireAuthorization()
            .Produces<ApiResponse<CreateSupplierResponse>>(StatusCodes.Status201Created)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest);

        group.MapGet("/", SearchSuppliers)
            .WithName("SearchSuppliers").WithTags("Suppliers").RequireAuthorization()
            .Produces<ApiResponse<PagedResult<SearchSuppliersResponse>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest);

        group.MapGet("/{id:guid}", GetSupplierById)
            .WithName("GetSupplierById").WithTags("Suppliers").RequireAuthorization()
            .Produces<ApiResponse<GetSupplierByIdResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}", UpdateSupplier)
            .WithName("UpdateSupplier").WithTags("Suppliers").RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", DeleteSupplier)
            .WithName("DeleteSupplier").WithTags("Suppliers").RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<object>>(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/restore", RestoreSupplier)
            .WithName("RestoreSupplier").WithTags("Suppliers").RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);
    }

    private async Task<IResult> CreateSupplier(
        [FromBody] CreateSupplierRequest request,
        ISender sender,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateSupplierCommand(
            currentUser.TenantId,
            request.Code,
            request.Name,
            request.Contact,
            request.Phone,
            request.Email,
            request.Address), cancellationToken);

        return Results.Created($"/api/v1/suppliers/{result.Id}", ApiResponse<CreateSupplierResponse>.Ok(result));
    }

    private async Task<IResult> GetSupplierById(
        Guid id,
        ISender sender,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetSupplierByIdQuery(currentUser.TenantId, id), cancellationToken);
        return Results.Ok(ApiResponse<GetSupplierByIdResponse>.Ok(result));
    }

    private async Task<IResult> SearchSuppliers(
        [FromQuery] string? search,
        [FromQuery] int page,
        [FromQuery] int limit,
        ISender sender,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SearchSuppliersQuery(
            currentUser.TenantId,
            search,
            page,
            limit), cancellationToken);

        return Results.Ok(ApiResponse<PagedResult<SearchSuppliersResponse>>.Ok(result));
    }

    private async Task<IResult> UpdateSupplier(
        Guid id,
        [FromBody] UpdateSupplierRequest request,
        ISender sender,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateSupplierCommand(
            currentUser.TenantId,
            id,
            request.Name,
            request.Contact,
            request.Phone,
            request.Email,
            request.Address), cancellationToken);

        return Results.NoContent();
    }

    private async Task<IResult> DeleteSupplier(
        Guid id,
        ISender sender,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteSupplierCommand(currentUser.TenantId, id), cancellationToken);
        return Results.NoContent();
    }

    private async Task<IResult> RestoreSupplier(
        Guid id,
        ISender sender,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        await sender.Send(new RestoreSupplierCommand(currentUser.TenantId, id), cancellationToken);
        return Results.NoContent();
    }
}
