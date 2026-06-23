using MediatR;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Common.Models;
using WMS.Application.Customers.Commands.CreateCustomer;
using WMS.Application.Customers.Commands.DeleteCustomer;
using WMS.Application.Customers.Commands.RestoreCustomer;
using WMS.Application.Customers.Commands.UpdateCustomer;
using WMS.Application.Customers.DTOs;
using WMS.Application.Customers.Queries.GetCustomerById;
using WMS.Application.Customers.Queries.SearchCustomers;
using WMS.Domain.Interfaces;

namespace DP.AppWMS.ApiService.Endpoints.Customers;

public sealed class CustomerEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiRoutes.Groups.Customers);

        group.MapPost("/", CreateCustomer)
            .WithName("CreateCustomer").WithTags("Customers").RequireAuthorization()
            .Produces<ApiResponse<CreateCustomerResponse>>(StatusCodes.Status201Created)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest);

        group.MapGet("/", SearchCustomers)
            .WithName("SearchCustomers").WithTags("Customers").RequireAuthorization()
            .Produces<ApiResponse<PagedResult<SearchCustomersResponse>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest);

        group.MapGet("/{id:guid}", GetCustomerById)
            .WithName("GetCustomerById").WithTags("Customers").RequireAuthorization()
            .Produces<ApiResponse<GetCustomerByIdResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}", UpdateCustomer)
            .WithName("UpdateCustomer").WithTags("Customers").RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", DeleteCustomer)
            .WithName("DeleteCustomer").WithTags("Customers").RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<object>>(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/restore", RestoreCustomer)
            .WithName("RestoreCustomer").WithTags("Customers").RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);
    }

    private async Task<IResult> CreateCustomer(
        [FromBody] CreateCustomerRequest request,
        ISender sender,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateCustomerCommand(
            currentUser.TenantId,
            request.Code,
            request.Name,
            request.Address,
            request.Phone,
            request.Type), cancellationToken);

        return Results.Created($"/api/v1/customers/{result.Id}", ApiResponse<CreateCustomerResponse>.Ok(result));
    }

    private async Task<IResult> GetCustomerById(
        Guid id,
        ISender sender,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCustomerByIdQuery(currentUser.TenantId, id), cancellationToken);
        return Results.Ok(ApiResponse<GetCustomerByIdResponse>.Ok(result));
    }

    private async Task<IResult> SearchCustomers(
        [FromQuery] string? search,
        [FromQuery] int page,
        [FromQuery] int limit,
        ISender sender,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SearchCustomersQuery(
            currentUser.TenantId,
            search,
            page,
            limit), cancellationToken);

        return Results.Ok(ApiResponse<PagedResult<SearchCustomersResponse>>.Ok(result));
    }

    private async Task<IResult> UpdateCustomer(
        Guid id,
        [FromBody] UpdateCustomerRequest request,
        ISender sender,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateCustomerCommand(
            id,
            currentUser.TenantId,
            request.Name,
            request.Address,
            request.Phone,
            request.Type), cancellationToken);

        return Results.NoContent();
    }

    private async Task<IResult> DeleteCustomer(
        Guid id,
        ISender sender,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteCustomerCommand(currentUser.TenantId, id), cancellationToken);
        return Results.NoContent();
    }

    private async Task<IResult> RestoreCustomer(
        Guid id,
        ISender sender,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        await sender.Send(new RestoreCustomerCommand(currentUser.TenantId, id), cancellationToken);
        return Results.NoContent();
    }
}
