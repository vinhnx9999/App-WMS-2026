using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;

namespace WMS.Application.Common.DynamicSearch;

public class QueryCollection : Collection<SearchObject>
{
    /// <summary>
    /// Expression returning true constant
    /// </summary>
    public static Expression<Func<T, bool>> True<T>() { return f => true; }

    /// <summary>
    /// Builds a combined dynamic Linq expression from this collection of search criteria.
    /// </summary>
    public Expression<Func<T, bool>>? AsExpression<T>(Condition? condition = Condition.AndAlso) where T : class
    {
        if (this.Count == 0)
        {
            return True<T>();
        }

        var parameter = Expression.Parameter(typeof(T), "m");
        Expression? expression = null;

        Expression append(Expression? exp1, Expression exp2)
        {
            if (exp1 == null)
            {
                return exp2;
            }
            return (condition ?? Condition.AndAlso) == Condition.OrElse
                ? Expression.OrElse(exp1, exp2)
                : Expression.AndAlso(exp1, exp2);
        }


        foreach (var item in this)
        {
            if (string.IsNullOrWhiteSpace(item.Name))
            {
                continue;
            }

            // Support nested property paths (e.g. "Product.Category.Name")
            Expression propertyExp = parameter;
            Type currentType = typeof(T);
            PropertyInfo? property = null;
            var parts = item.Name.Split('.');

            foreach (var part in parts)
            {
                property = currentType.GetProperty(part, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (property == null || !property.CanRead)
                {
                    property = null;
                    break;
                }
                propertyExp = Expression.Property(propertyExp, property);
                currentType = property.PropertyType;
            }

            if (property == null || (string.IsNullOrEmpty(item.Text) && item.Value == null))
            {
                continue;
            }

            // Sync Text and Value
            if (string.IsNullOrEmpty(item.Text) && item.Value != null)
            {
                item.Text = item.Value.ToString() ?? string.Empty;
            }

            Type realType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

            // Handle date time rounding for boundary checks
            if ((realType == typeof(DateTime) || realType == typeof(DateTimeOffset))
                && (item.Operator == Operators.LessThanOrEqual || item.Operator == Operators.LessThan))
            {
                if (DateTime.TryParse(item.Text, out var parsedDate))
                {
                    // Round up to the very end of the day: 23:59:59.999
                    item.Text = parsedDate.Date.AddDays(1).AddTicks(-1).ToString("o");
                }
            }

            // Parse text input to underlying type safely
            if (!string.IsNullOrEmpty(item.Text))
            {
                try
                {
                    if (realType.IsEnum)
                    {
                        item.Value = Enum.Parse(realType, item.Text, ignoreCase: true);
                    }
                    else if (realType == typeof(Guid))
                    {
                        if (Guid.TryParse(item.Text, out var guidVal))
                        {
                            item.Value = guidVal;
                        }
                    }
                    else if (realType == typeof(DateOnly))
                    {
                        if (DateOnly.TryParse(item.Text, out var dateVal))
                        {
                            item.Value = dateVal;
                        }
                    }
                    else
                    {
                        item.Value = Convert.ChangeType(item.Text, realType);
                    }
                }
                catch
                {
                    // Skip filter criteria if value conversion fails
                    continue;
                }
            }

            Expression<Func<object?>> valueLambda = () => item.Value;
            var convertedValue = Expression.Convert(valueLambda.Body, property.PropertyType);

            switch (item.Operator)
            {
                case Operators.Equal:
                    expression = append(expression, Expression.Equal(propertyExp, convertedValue));
                    break;
                case Operators.GreaterThan:
                    expression = append(expression, Expression.GreaterThan(propertyExp, convertedValue));
                    break;
                case Operators.GreaterThanOrEqual:
                    expression = append(expression, Expression.GreaterThanOrEqual(propertyExp, convertedValue));
                    break;
                case Operators.LessThan:
                    expression = append(expression, Expression.LessThan(propertyExp, convertedValue));
                    break;
                case Operators.LessThanOrEqual:
                    expression = append(expression, Expression.LessThanOrEqual(propertyExp, convertedValue));
                    break;
                case Operators.Contains:
                    if (realType == typeof(string))
                    {
                        var isNullOrEmptyMethod = typeof(string).GetMethod("IsNullOrEmpty", new[] { typeof(string) });
                        var toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes);
                        var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });

                        if (isNullOrEmptyMethod != null && toLowerMethod != null && containsMethod != null)
                        {
                            // !string.IsNullOrEmpty(property)
                            var notNullCheck = Expression.Not(Expression.Call(null, isNullOrEmptyMethod, propertyExp));

                            // property.ToLower()
                            var propertyToLower = Expression.Call(propertyExp, toLowerMethod);

                            // Constant lower value
                            var lowerValue = item.Value?.ToString()?.ToLower() ?? string.Empty;
                            var constantLower = Expression.Constant(lowerValue, typeof(string));

                            // property.ToLower().Contains(value.ToLower())
                            var containsCall = Expression.Call(propertyToLower, containsMethod, constantLower);

                            // !string.IsNullOrEmpty(property) && property.ToLower().Contains(value.ToLower())
                            var finalContainsExp = Expression.AndAlso(notNullCheck, containsCall);

                            expression = append(expression, finalContainsExp);
                        }
                    }
                    break;
            }
        }

        if (expression == null)
        {
            return True<T>();
        }

        return Expression.Lambda<Func<T, bool>>(expression, parameter);
    }
}
