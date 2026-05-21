using Microsoft.EntityFrameworkCore;

namespace WMS.Application.Common.Specifications;

public static class SpecificationEvaluator
{
    public static IQueryable<T> GetQuery<T>(
        IQueryable<T> inputQuery,
        ISpecification<T> specification,
        bool applyPaging = true)
        where T : class
    {
        var query = inputQuery;

        if (specification.Criteria is not null)
        {
            query = query.Where(specification.Criteria);
        }

        query = specification.Includes.Aggregate(
            query,
            (current, include) => current.Include(include));

        if (specification.OrderBy is not null)
        {
            query = query.OrderBy(specification.OrderBy);
        }
        else if (specification.OrderByDescending is not null)
        {
            query = query.OrderByDescending(specification.OrderByDescending);
        }

        if (applyPaging && specification.IsPagingEnabled)
        {
            query = query.Skip(specification.Skip!.Value).Take(specification.Take!.Value);
        }

        return query;
    }
}
