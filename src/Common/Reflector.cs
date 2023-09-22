using System.Linq.Expressions;
using System.Reflection;
using Common.Extensions;

namespace Common;

/// <summary>
///     Provides information about types at runtime
/// </summary>
public static class Reflector
{
    /// <summary>
    ///     Gets the name of the property represented by the lambda expression.
    /// </summary>
    public static string GetPropertyName<TTarget, TResult>(Expression<Func<TTarget, TResult>> property)
    {
        return GetProperty(property).Name;
    }

    private static PropertyInfo GetProperty<TTarget, TResult>(Expression<Func<TTarget, TResult>> property)
    {
        var info = GetMemberInfo(property) as PropertyInfo;
        if (info.NotExists())
        {
            throw new ArgumentException(Resources.Reflector_ErrorNotProperty);
        }

        return info!;
    }

    private static MemberInfo GetMemberInfo(LambdaExpression lambda)
    {
        if (lambda.Body.NodeType == ExpressionType.Convert && lambda.Body.Type == typeof(object))
        {
            var expression = Expression.Lambda(((UnaryExpression)lambda.Body).Operand, lambda.Parameters);
            if (expression.NodeType == ExpressionType.Lambda)
            {
                return ((MemberExpression)expression.Body).Member;
            }
        }

        if (lambda.Body.NodeType == ExpressionType.MemberAccess)
        {
            return ((MemberExpression)lambda.Body).Member;
        }

        throw new ArgumentException(Resources.Reflector_ErrorNotMemberAccess, nameof(lambda));
    }
}