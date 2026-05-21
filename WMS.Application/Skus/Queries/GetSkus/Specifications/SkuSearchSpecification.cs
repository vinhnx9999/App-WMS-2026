using System.Linq.Expressions;
using WMS.Application.Common.Specifications;
using WMS.Domain.Entities;

namespace WMS.Application.Skus.Queries.GetSkus.Specifications;

public sealed class SkuSearchSpecification : Specification<SkuEntity>
{
    public SkuSearchSpecification(
        Guid tenantId,
        string? search,
        Guid? categoryId,
        int page,
        int limit)
    {
        AddCriteria(x => x.TenantId == tenantId && !x.IsDeleted);

        if (categoryId.HasValue)
        {
            AddCriteria(x => x.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLower();

            Expression<Func<SkuEntity, bool>> searchCriteria = x =>
                x.SkuCode.ToLower().Contains(keyword) ||
                x.Name.ToLower().Contains(keyword) ||
                (x.Description != null && x.Description.ToLower().Contains(keyword));

            AddCriteria(searchCriteria);
        }

        AddInclude(x => x.Category!);
        ApplyOrderByDescending(x => x.UpdatedAt);
        ApplyPaging((page - 1) * limit, limit);
    }
}
