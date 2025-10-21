using System.Linq.Expressions;

namespace Raziee.SharedKernel.Specifications;

/// <summary>
/// Extension methods for combining expressions.
/// </summary>
public static class ExpressionExtensions
{
    /// <summary>
    /// Combines two expressions using the AND operator.
    /// </summary>
    /// <typeparam name="T">The type of the parameter</typeparam>
    /// <param name="left">The left expression</param>
    /// <param name="right">The right expression</param>
    /// <returns>A combined expression</returns>
    public static Expression<Func<T, bool>> And<T>(
        this Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right
    )
    {
        var parameter = Expression.Parameter(typeof(T));
        var leftVisitor = new ReplaceExpressionVisitor(left.Parameters[0], parameter);
        var rightVisitor = new ReplaceExpressionVisitor(right.Parameters[0], parameter);
        var leftExpression = leftVisitor.Visit(left.Body);
        var rightExpression = rightVisitor.Visit(right.Body);
        return Expression.Lambda<Func<T, bool>>(
            Expression.AndAlso(leftExpression!, rightExpression!),
            parameter
        );
    }

    /// <summary>
    /// Combines two expressions using the OR operator.
    /// </summary>
    /// <typeparam name="T">The type of the parameter</typeparam>
    /// <param name="left">The left expression</param>
    /// <param name="right">The right expression</param>
    /// <returns>A combined expression</returns>
    public static Expression<Func<T, bool>> Or<T>(
        this Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right
    )
    {
        var parameter = Expression.Parameter(typeof(T));
        var leftVisitor = new ReplaceExpressionVisitor(left.Parameters[0], parameter);
        var rightVisitor = new ReplaceExpressionVisitor(right.Parameters[0], parameter);
        var leftExpression = leftVisitor.Visit(left.Body);
        var rightExpression = rightVisitor.Visit(right.Body);
        return Expression.Lambda<Func<T, bool>>(
            Expression.OrElse(leftExpression!, rightExpression!),
            parameter
        );
    }
}
