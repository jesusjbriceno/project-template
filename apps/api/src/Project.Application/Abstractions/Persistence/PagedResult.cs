namespace Project.Application.Abstractions.Persistence;

/// <summary>
/// Application-layer immutable paginated result. Contains the requested page of items
/// plus total-count metadata for client-side pagination controls.
/// Lives in the Application boundary — no EF Core, ASP.NET, or Infrastructure references.
/// </summary>
/// <typeparam name="T">The type of items in the page.</typeparam>
public sealed class PagedResult<T>
{
    /// <summary>
    /// Items on the current page.
    /// </summary>
    public IReadOnlyCollection<T> Items { get; }

    /// <summary>
    /// Total number of items across all pages.
    /// </summary>
    public int TotalCount { get; }

    /// <summary>
    /// 1-based current page number.
    /// </summary>
    public int Page { get; }

    /// <summary>
    /// Maximum items per page (as requested).
    /// </summary>
    public int PageSize { get; }

    /// <summary>
    /// Total number of pages, computed as ceiling(TotalCount / PageSize).
    /// </summary>
    public int TotalPages => PageSize > 0
        ? (int)Math.Ceiling((double)TotalCount / PageSize)
        : 0;

    /// <summary>
    /// Whether there is a page after the current one.
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Whether there is a page before the current one.
    /// </summary>
    public bool HasPreviousPage => Page > 1;

    public PagedResult(IReadOnlyCollection<T> Items, int TotalCount, int Page, int PageSize)
    {
        ArgumentNullException.ThrowIfNull(Items, nameof(Items));
        ArgumentOutOfRangeException.ThrowIfNegative(TotalCount, nameof(TotalCount));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(Page, 0, nameof(Page));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(PageSize, 0, nameof(PageSize));

        this.Items = Items;
        this.TotalCount = TotalCount;
        this.Page = Page;
        this.PageSize = PageSize;
    }
}
