using System.Linq.Expressions;
using WMS.Application.Common.Specifications;
using WMS.Domain.Entities;

namespace WMS.Application.Skus.Queries.SearchSkus;

public sealed class SearchSkusSpecification : Specification<SkuEntity>
{
    public SearchSkusSpecification(
        Guid tenantId,
        string? search,
        Guid? categoryId,
        int page,
        int limit)
    {
        AddCriteria(x => x.TenantId == tenantId && x.DeletedAt == null);

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

        ApplyOrderByDescending(x => x.UpdatedAt);
        ApplyPaging((page - 1) * limit, limit);
    }
}
