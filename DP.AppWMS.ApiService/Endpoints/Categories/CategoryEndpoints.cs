using MediatR;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Categories.Commands.CreateCategory;
using WMS.Application.Categories.Commands.UpdateCategory;
using WMS.Application.Categories.Commands.DeleteCategory;
using WMS.Application.Categories.Commands.RestoreCategory;
using WMS.Application.Categories.Queries.GetCategoryById;
using WMS.Application.Categories.Queries.SearchCategories;
using WMS.Application.Categories.DTOs;
using WMS.Application.Common.Models;
using WMS.Domain.Interfaces;

namespace DP.AppWMS.ApiService.Endpoints.Categories;

public sealed class CategoryEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiRoutes.Groups.Categories);

        group.MapPost("/", CreateCategory)
            .WithName("CreateCategory").WithTags("Categories").RequireAuthorization()
            .Produces<ApiResponse<CreateCategoryResponse>>(StatusCodes.Status201Created)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest);

        group.MapGet("/", SearchCategories)
            .WithName("SearchCategories").WithTags("Categories").RequireAuthorization()
            .Produces<ApiResponse<PagedResult<SearchCategoriesResponse>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest);

        group.MapGet("/{id:guid}", GetCategoryById)
            .WithName("GetCategoryById").WithTags("Categories").RequireAuthorization()
            .Produces<ApiResponse<GetCategoryByIdResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}", UpdateCategory)
            .WithName("UpdateCategory").WithTags("Categories").RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", DeleteCategory)
            .WithName("DeleteCategory").WithTags("Categories").RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<object>>(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/restore", RestoreCategory)
            .WithName("RestoreCategory").WithTags("Categories").RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);
    }

    private async Task<IResult> CreateCategory(
        [FromBody] CreateCategoryRequest request,
        ISender sender,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateCategoryCommand(
            currentUser.TenantId,
            request.Name,
            request.Description), cancellationToken);

        return Results.Created($"/api/v1/categories/{result.Id}", ApiResponse<CreateCategoryResponse>.Ok(result));
    }

    private async Task<IResult> GetCategoryById(
        Guid id,
        ISender sender,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCategoryByIdQuery(currentUser.TenantId, id), cancellationToken);
        return Results.Ok(ApiResponse<GetCategoryByIdResponse>.Ok(result));
    }

    private async Task<IResult> SearchCategories(
        [FromQuery] string? search,
        [FromQuery] int page,
        [FromQuery] int limit,
        ISender sender,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SearchCategoriesQuery(
            currentUser.TenantId,
            search,
            page,
            limit), cancellationToken);

        return Results.Ok(ApiResponse<PagedResult<SearchCategoriesResponse>>.Ok(result));
    }

    private async Task<IResult> UpdateCategory(
        Guid id,
        [FromBody] UpdateCategoryRequest request,
        ISender sender,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateCategoryCommand(
            currentUser.TenantId,
            id,
            request.Name,
            request.Description), cancellationToken);

        return Results.NoContent();
    }

    private async Task<IResult> DeleteCategory(
        Guid id,
        ISender sender,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteCategoryCommand(currentUser.TenantId, id), cancellationToken);
        return Results.NoContent();
    }

    private async Task<IResult> RestoreCategory(
        Guid id,
        ISender sender,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        await sender.Send(new RestoreCategoryCommand(currentUser.TenantId, id), cancellationToken);
        return Results.NoContent();
    }
}
