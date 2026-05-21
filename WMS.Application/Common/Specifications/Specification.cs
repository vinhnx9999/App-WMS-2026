using System.Linq.Expressions;

namespace WMS.Application.Common.Specifications;

public abstract class Specification<T> : ISpecification<T>
{
    public Expression<Func<T, bool>>? Criteria { get; private set; }
    public List<Expression<Func<T, object>>> Includes { get; } = [];
    public Expression<Func<T, object>>? OrderBy { get; private set; }
    public Expression<Func<T, object>>? OrderByDescending { get; private set; }
    public int? Skip { get; private set; }
    public int? Take { get; private set; }
    public bool IsPagingEnabled { get; private set; }

    protected void AddCriteria(Expression<Func<T, bool>> criteria)
    {
        Criteria = Criteria is null ? criteria : Criteria.And(criteria);
    }

    protected void AddInclude(Expression<Func<T, object>> include)
    {
        Includes.Add(include);
    }

    protected void ApplyOrderBy(Expression<Func<T, object>> orderBy)
    {
        OrderBy = orderBy;
        OrderByDescending = null;
    }

    protected void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescending)
    {
        OrderByDescending = orderByDescending;
        OrderBy = null;
    }

    protected void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
        IsPagingEnabled = true;
    }
}
