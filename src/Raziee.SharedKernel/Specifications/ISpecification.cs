using System.Linq.Expressions;

namespace Raziee.SharedKernel.Specifications;

/// <summary>
/// Interface for specifications that define criteria for querying entities.
/// Specifications encapsulate business rules and can be combined to create complex queries.
/// </summary>
/// <typeparam name="TEntity">The type of entity</typeparam>
public interface ISpecification<TEntity>
{
    /// <summary>
    /// Gets the criteria expression for filtering entities.
    /// </summary>
    Expression<Func<TEntity, bool>>? Criteria { get; }

    /// <summary>
    /// Gets the list of include expressions for eager loading related entities.
    /// </summary>
    List<Expression<Func<TEntity, object>>> Includes { get; }

    /// <summary>
    /// Gets the list of string-based includes for eager loading related entities.
    /// </summary>
    List<string> IncludeStrings { get; }

    /// <summary>
    /// Gets the expression for ordering entities.
    /// </summary>
    Expression<Func<TEntity, object>>? OrderBy { get; }

    /// <summary>
    /// Gets the expression for ordering entities in descending order.
    /// </summary>
    Expression<Func<TEntity, object>>? OrderByDescending { get; }

    /// <summary>
    /// Gets the expression for then ordering entities.
    /// </summary>
    Expression<Func<TEntity, object>>? ThenBy { get; }

    /// <summary>
    /// Gets the expression for then ordering entities in descending order.
    /// </summary>
    Expression<Func<TEntity, object>>? ThenByDescending { get; }

    /// <summary>
    /// Gets the number of entities to take.
    /// </summary>
    int Take { get; }

    /// <summary>
    /// Gets the number of entities to skip.
    /// </summary>
    int Skip { get; }

    /// <summary>
    /// Gets a value indicating whether the query should be tracked by the change tracker.
    /// </summary>
    bool IsTrackingEnabled { get; }

    /// <summary>
    /// Gets a value indicating whether the query should ignore query filters.
    /// </summary>
    bool IsIgnoreQueryFilters { get; }
}
