using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Domain.Entities;
using WMS.Domain.Entities.Product;
using WMS.Domain.Interfaces;

namespace WMS.Application.Categories.Commands.DeleteCategory;

public sealed record DeleteCategoryCommand(
    Guid TenantId,
    Guid Id) : IRequest;

public sealed class DeleteCategoryCommandHandler(IUnitOfWork uow) : IRequestHandler<DeleteCategoryCommand>
{
    private readonly IUnitOfWork _uow = uow;

    public async Task Handle(DeleteCategoryCommand request, CancellationToken ct)
    {
        var category = await _uow.Repository<Category>().Query()
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.TenantId == request.TenantId && !x.IsDeleted, ct);

        if (category == null)
        {
            throw new AppException(404, "CATEGORY_NOT_FOUND", "Category not found.");
        }

        // Check active products
        var hasActiveProducts = await _uow.Repository<Product>().Query()
            .AnyAsync(x => x.CategoryId == request.Id && x.TenantId == request.TenantId && !x.IsDeleted, ct);

        if (hasActiveProducts)
        {
            throw new AppException(400, "CATEGORY_IN_USE", "Cannot delete category because it is associated with active products.");
        }

        category.MarkDeleted();
        await _uow.SaveChangesAsync(ct);
    }
}
