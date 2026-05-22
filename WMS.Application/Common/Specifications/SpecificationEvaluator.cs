using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

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
            (current, include) => current.Include(GetIncludePath(include)));

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

    private static string GetIncludePath<T>(Expression<Func<T, object>> include)
    {
        var body = include.Body is UnaryExpression unary ? unary.Operand : include.Body;

        if (body is MemberExpression member)
        {
            return member.Member.Name;
        }

        throw new InvalidOperationException($"Invalid include expression: {include}");
    }
}
