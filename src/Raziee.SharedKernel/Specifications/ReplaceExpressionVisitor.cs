using System.Linq.Expressions;

namespace Raziee.SharedKernel.Specifications;

/// <summary>
/// Helper class for replacing expressions in expression trees.
/// </summary>
public class ReplaceExpressionVisitor : ExpressionVisitor
{
    private readonly Expression _oldValue;
    private readonly Expression _newValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReplaceExpressionVisitor"/> class.
    /// </summary>
    /// <param name="oldValue">The old expression to replace</param>
    /// <param name="newValue">The new expression to replace with</param>
    public ReplaceExpressionVisitor(Expression oldValue, Expression newValue)
    {
        _oldValue = oldValue;
        _newValue = newValue;
    }

    /// <summary>
    /// Visits the expression and replaces the old value with the new value.
    /// </summary>
    /// <param name="node">The expression node to visit</param>
    /// <returns>The visited expression</returns>
    public override Expression? Visit(Expression? node)
    {
        return node == _oldValue ? _newValue : base.Visit(node);
    }
}
