using System.Linq.Expressions;

namespace WMS.Application.Common.Specifications;

public static class ExpressionExtensions
{
    public static Expression<Func<T, bool>> And<T>(
        this Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
    {
        return left.Compose(right, Expression.AndAlso);
    }

    public static Expression<Func<T, bool>> Or<T>(
        this Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
    {
        return left.Compose(right, Expression.OrElse);
    }

    private static Expression<Func<T, bool>> Compose<T>(
        this Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right,
        Func<Expression, Expression, BinaryExpression> merge)
    {
        var parameter = Expression.Parameter(typeof(T));
        var leftBody = new ReplaceParameterVisitor(left.Parameters[0], parameter).Visit(left.Body)!;
        var rightBody = new ReplaceParameterVisitor(right.Parameters[0], parameter).Visit(right.Body)!;

        return Expression.Lambda<Func<T, bool>>(merge(leftBody, rightBody), parameter);
    }

    private sealed class ReplaceParameterVisitor(
        ParameterExpression source,
        ParameterExpression target) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == source ? target : node;
        }
    }
}
