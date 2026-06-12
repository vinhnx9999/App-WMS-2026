using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Categories.DTOs;
using WMS.Domain.Entities;
using WMS.Domain.Interfaces;

namespace WMS.Application.Categories.Queries.GetCategoryById;

public sealed record GetCategoryByIdQuery(
    Guid TenantId,
    Guid Id) : IRequest<GetCategoryByIdResponse>;

public sealed class GetCategoryByIdQueryHandler(IUnitOfWork uow)
    : IRequestHandler<GetCategoryByIdQuery, GetCategoryByIdResponse>
{
    private readonly IUnitOfWork _uow = uow;

    public async Task<GetCategoryByIdResponse> Handle(GetCategoryByIdQuery request, CancellationToken ct)
    {
        var category = await _uow.Repository<Category>().Query()
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.TenantId == request.TenantId && !x.IsDeleted, ct);

        if (category == null)
        {
            throw new AppException(404, "CATEGORY_NOT_FOUND", "Category not found.");
        }

        return new GetCategoryByIdResponse(
            category.Id,
            category.TenantId,
            category.Name,
            category.Slug,
            category.Description,
            category.CreatedAt,
            category.UpdatedAt);
    }
}
