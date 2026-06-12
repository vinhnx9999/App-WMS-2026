using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Application.Categories.DTOs;
using WMS.Domain.Entities;
using WMS.Domain.Interfaces;

namespace WMS.Application.Categories.Commands.CreateCategory;

public sealed record CreateCategoryCommand(
    Guid TenantId,
    string Name,
    string? Description) : IRequest<CreateCategoryResponse>;

public sealed class CreateCategoryCommandHandler(IUnitOfWork uow)
    : IRequestHandler<CreateCategoryCommand, CreateCategoryResponse>
{
    private readonly IUnitOfWork _uow = uow;

    public async Task<CreateCategoryResponse> Handle(CreateCategoryCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new AppException(400, "VALIDATION_FAILED", "Category name is required.");
        }

        var trimmedName = request.Name.Trim();
        if (trimmedName.Length > 200)
        {
            throw new AppException(400, "VALIDATION_FAILED", "Category name must not exceed 200 characters.");
        }

        if (request.Description != null && request.Description.Length > 500)
        {
            throw new AppException(400, "VALIDATION_FAILED", "Category description must not exceed 500 characters.");
        }

        var category = Category.Create(request.TenantId, trimmedName, request.Description);

        await _uow.Repository<Category>().AddAsync(category, ct);
        await _uow.SaveChangesAsync(ct);

        return new CreateCategoryResponse(
            category.Id,
            category.TenantId,
            category.Name,
            category.Slug,
            category.Description,
            category.CreatedAt,
            category.UpdatedAt);
    }
}
