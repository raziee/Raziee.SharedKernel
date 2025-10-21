namespace Raziee.SharedKernel.Repositories;

/// <summary>
/// Represents a paginated result containing data and pagination information.
/// </summary>
/// <typeparam name="T">The type of items in the result</typeparam>
public class PaginatedResult<T>
{
    /// <summary>
    /// Gets the items in the current page.
    /// </summary>
    public IEnumerable<T> Items { get; }

    /// <summary>
    /// Gets the current page number.
    /// </summary>
    public int PageNumber { get; }

    /// <summary>
    /// Gets the page size.
    /// </summary>
    public int PageSize { get; }

    /// <summary>
    /// Gets the total number of items.
    /// </summary>
    public long TotalCount { get; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// Gets a value indicating whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// Gets a value indicating whether there is a next page.
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>
    /// Initializes a new instance of the <see cref="PaginatedResult{T}"/> class.
    /// </summary>
    /// <param name="items">The items in the current page</param>
    /// <param name="pageNumber">The current page number</param>
    /// <param name="pageSize">The page size</param>
    /// <param name="totalCount">The total number of items</param>
    public PaginatedResult(IEnumerable<T> items, int pageNumber, int pageSize, long totalCount)
    {
        Items = items ?? throw new ArgumentNullException(nameof(items));
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalCount = totalCount;
    }
}
