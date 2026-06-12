using MediatR;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Models;
using WMS.Domain.Entities;
using WMS.Domain.Interfaces;

namespace WMS.Application.Categories.Commands.RestoreCategory;

public sealed record RestoreCategoryCommand(
    Guid TenantId,
    Guid Id) : IRequest;

public sealed class RestoreCategoryCommandHandler(IUnitOfWork uow) : IRequestHandler<RestoreCategoryCommand>
{
    private readonly IUnitOfWork _uow = uow;

    public async Task Handle(RestoreCategoryCommand request, CancellationToken ct)
    {
        var category = await _uow.Repository<Category>().Query()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.TenantId == request.TenantId, ct);

        if (category == null)
        {
            throw new AppException(404, "CATEGORY_NOT_FOUND", "Category not found.");
        }

        if (!category.IsDeleted)
        {
            throw new AppException(400, "CATEGORY_NOT_DELETED", "Only deleted categories can be restored.");
        }

        category.MarkRestored();
        await _uow.SaveChangesAsync(ct);
    }
}
