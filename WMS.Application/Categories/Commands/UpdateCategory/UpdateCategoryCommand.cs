using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Domain.Entities;
using WMS.Domain.Interfaces;

namespace WMS.Application.Categories.Commands.UpdateCategory;

public sealed record UpdateCategoryCommand(
    Guid TenantId,
    Guid Id,
    string Name,
    string? Description) : IRequest;

public sealed class UpdateCategoryCommandHandler(IUnitOfWork uow) : IRequestHandler<UpdateCategoryCommand>
{
    private readonly IUnitOfWork _uow = uow;

    public async Task Handle(UpdateCategoryCommand request, CancellationToken ct)
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

        var category = await _uow.Repository<Category>().Query()
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.TenantId == request.TenantId && !x.IsDeleted, ct);

        if (category == null)
        {
            throw new AppException(404, "CATEGORY_NOT_FOUND", "Category not found.");
        }

        category.Update(trimmedName, request.Description);

        await _uow.SaveChangesAsync(ct);
    }
}
