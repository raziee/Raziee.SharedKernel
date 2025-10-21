using System.Linq.Expressions;

namespace Raziee.SharedKernel.Specifications;

/// <summary>
/// Base class for specifications that define criteria for querying entities.
/// Provides a fluent interface for building specifications.
/// </summary>
/// <typeparam name="TEntity">The type of entity</typeparam>
/// <typeparam name="TId">The type of the entity's identifier</typeparam>
public abstract class BaseSpecification<TEntity, TId> : ISpecification<TEntity>
    where TEntity : Domain.Entities.Entity<TId>
    where TId : notnull
{
    /// <summary>
    /// Gets the criteria expression for filtering entities.
    /// </summary>
    public Expression<Func<TEntity, bool>>? Criteria { get; private set; }

    /// <summary>
    /// Gets the list of include expressions for eager loading related entities.
    /// </summary>
    public List<Expression<Func<TEntity, object>>> Includes { get; } = new();

    /// <summary>
    /// Gets the list of string-based includes for eager loading related entities.
    /// </summary>
    public List<string> IncludeStrings { get; } = new();

    /// <summary>
    /// Gets the expression for ordering entities.
    /// </summary>
    public Expression<Func<TEntity, object>>? OrderBy { get; private set; }

    /// <summary>
    /// Gets the expression for ordering entities in descending order.
    /// </summary>
    public Expression<Func<TEntity, object>>? OrderByDescending { get; private set; }

    /// <summary>
    /// Gets the expression for then ordering entities.
    /// </summary>
    public Expression<Func<TEntity, object>>? ThenBy { get; private set; }

    /// <summary>
    /// Gets the expression for then ordering entities in descending order.
    /// </summary>
    public Expression<Func<TEntity, object>>? ThenByDescending { get; private set; }

    /// <summary>
    /// Gets the number of entities to take.
    /// </summary>
    public int Take { get; private set; }

    /// <summary>
    /// Gets the number of entities to skip.
    /// </summary>
    public int Skip { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the query should be tracked by the change tracker.
    /// </summary>
    public bool IsTrackingEnabled { get; private set; } = true;

    /// <summary>
    /// Gets a value indicating whether the query should ignore query filters.
    /// </summary>
    public bool IsIgnoreQueryFilters { get; private set; }

    /// <summary>
    /// Adds a criteria expression to the specification.
    /// </summary>
    /// <param name="criteria">The criteria expression</param>
    protected void AddCriteria(Expression<Func<TEntity, bool>> criteria)
    {
        Criteria = Criteria == null ? criteria : Criteria.And(criteria);
    }

    /// <summary>
    /// Adds an include expression to the specification.
    /// </summary>
    /// <param name="includeExpression">The include expression</param>
    protected void AddInclude(Expression<Func<TEntity, object>> includeExpression)
    {
        Includes.Add(includeExpression);
    }

    /// <summary>
    /// Adds a string-based include to the specification.
    /// </summary>
    /// <param name="includeString">The include string</param>
    protected void AddInclude(string includeString)
    {
        IncludeStrings.Add(includeString);
    }

    /// <summary>
    /// Sets the ordering expression for the specification.
    /// </summary>
    /// <param name="orderByExpression">The ordering expression</param>
    protected void ApplyOrderBy(Expression<Func<TEntity, object>> orderByExpression)
    {
        OrderBy = orderByExpression;
    }

    /// <summary>
    /// Sets the descending ordering expression for the specification.
    /// </summary>
    /// <param name="orderByDescendingExpression">The descending ordering expression</param>
    protected void ApplyOrderByDescending(
        Expression<Func<TEntity, object>> orderByDescendingExpression
    )
    {
        OrderByDescending = orderByDescendingExpression;
    }

    /// <summary>
    /// Sets the then ordering expression for the specification.
    /// </summary>
    /// <param name="thenByExpression">The then ordering expression</param>
    protected void ApplyThenBy(Expression<Func<TEntity, object>> thenByExpression)
    {
        ThenBy = thenByExpression;
    }

    /// <summary>
    /// Sets the then descending ordering expression for the specification.
    /// </summary>
    /// <param name="thenByDescendingExpression">The then descending ordering expression</param>
    protected void ApplyThenByDescending(
        Expression<Func<TEntity, object>> thenByDescendingExpression
    )
    {
        ThenByDescending = thenByDescendingExpression;
    }

    /// <summary>
    /// Sets the pagination for the specification.
    /// </summary>
    /// <param name="skip">The number of entities to skip</param>
    /// <param name="take">The number of entities to take</param>
    protected void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
    }

    /// <summary>
    /// Enables or disables change tracking for the specification.
    /// </summary>
    /// <param name="isTrackingEnabled">True to enable tracking; otherwise, false</param>
    protected void ApplyTracking(bool isTrackingEnabled)
    {
        IsTrackingEnabled = isTrackingEnabled;
    }

    /// <summary>
    /// Enables or disables query filters for the specification.
    /// </summary>
    /// <param name="isIgnoreQueryFilters">True to ignore query filters; otherwise, false</param>
    protected void ApplyIgnoreQueryFilters(bool isIgnoreQueryFilters)
    {
        IsIgnoreQueryFilters = isIgnoreQueryFilters;
    }
}
