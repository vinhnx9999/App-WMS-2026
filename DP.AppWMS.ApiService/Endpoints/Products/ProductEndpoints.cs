using MediatR;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Common.Models;
using WMS.Application.Products.Commands.CreateProduct;
using WMS.Application.Products.Commands.DeleteProduct;
using WMS.Application.Products.Commands.RestoreProduct;
using WMS.Application.Products.Commands.UpdateProduct;
using WMS.Application.Products.DTOs;
using WMS.Application.Products.Queries.GetProductById;
using WMS.Application.Products.Queries.SearchProducts;
using WMS.Domain.Interfaces;

namespace DP.AppWMS.ApiService.Endpoints.Products;

public sealed class ProductEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiRoutes.Groups.Products);

        group.MapPost("/", CreateProduct)
            .WithName("CreateProduct").WithTags("Products").RequireAuthorization()
            .Produces<ApiResponse<CreateProductResponse>>(StatusCodes.Status201Created)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status409Conflict);

        group.MapGet("/", SearchProducts)
            .WithName("SearchProducts").WithTags("Products").RequireAuthorization()
            .Produces<ApiResponse<PagedResult<SearchProductsResponse>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest);

        group.MapGet("/{id:guid}", GetProductById)
            .WithName("GetProductById").WithTags("Products").RequireAuthorization()
            .Produces<ApiResponse<GetProductByIdResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}", UpdateProduct)
            .WithName("UpdateProduct").WithTags("Products").RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", DeleteProduct)
            .WithName("DeleteProduct").WithTags("Products").RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<object>>(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/restore", RestoreProduct)
            .WithName("RestoreProduct").WithTags("Products").RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);
    }

    private async Task<IResult> CreateProduct(
        [FromBody] CreateProductRequest request,
        ISender sender,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateProductCommand(
            currentUser.TenantId,
            request.ProductCode,
            request.ProductName,
            request.Description,
            request.CategoryId), cancellationToken);

        return Results.CreatedAtRoute(
            "GetProductById",
            new { id = result.Id },
            ApiResponse<CreateProductResponse>.Ok(result));
    }

    private async Task<IResult> GetProductById(
        Guid id,
        ISender sender,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetProductByIdQuery(currentUser.TenantId, id), cancellationToken);
        return Results.Ok(ApiResponse<GetProductByIdResponse>.Ok(result));
    }

    private async Task<IResult> SearchProducts(
        [FromQuery] string? search,
        [FromQuery] Guid? categoryId,
        [FromQuery] int page,
        [FromQuery] int limit,
        ISender sender,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SearchProductsQuery(
            currentUser.TenantId,
            search,
            categoryId,
            page,
            limit), cancellationToken);

        return Results.Ok(ApiResponse<PagedResult<SearchProductsResponse>>.Ok(result));
    }

    private async Task<IResult> UpdateProduct(
        Guid id,
        [FromBody] UpdateProductRequest request,
        ISender sender,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateProductCommand(
            currentUser.TenantId,
            id,
            request.ProductName,
            request.Description,
            request.CategoryId), cancellationToken);

        return Results.NoContent();
    }

    private async Task<IResult> DeleteProduct(
        Guid id,
        ISender sender,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteProductCommand(currentUser.TenantId, id), cancellationToken);
        return Results.NoContent();
    }

    private async Task<IResult> RestoreProduct(
        Guid id,
        ISender sender,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        await sender.Send(new RestoreProductCommand(currentUser.TenantId, id), cancellationToken);
        return Results.NoContent();
    }
}
