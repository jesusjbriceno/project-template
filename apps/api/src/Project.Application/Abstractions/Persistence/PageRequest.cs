namespace Project.Application.Abstractions.Persistence;

/// <summary>
/// Application-layer immutable pagination descriptor.
/// Lives in the Application boundary — no EF Core, ASP.NET, or Infrastructure references.
/// </summary>
public sealed record PageRequest
{
    /// <summary>
    /// Maximum number of items allowed in a single page.
    /// </summary>
    public const int MaxPageSize = 100;

    /// <summary>
    /// 1-based page number.
    /// </summary>
    public int Page { get; }

    /// <summary>
    /// Number of items per page (1..<see cref="MaxPageSize"/>).
    /// </summary>
    public int PageSize { get; }

    public PageRequest(int Page, int PageSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(Page, 0, nameof(Page));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(PageSize, 0, nameof(PageSize));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(PageSize, MaxPageSize, nameof(PageSize));

        this.Page = Page;
        this.PageSize = PageSize;
    }
}
