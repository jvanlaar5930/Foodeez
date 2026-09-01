namespace Foodeez.Application.Common;

/// <summary>
/// One page of results plus just enough to drive an infinite scroll.
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();

    /// <summary>1-based.</summary>
    public int Page { get; set; } = 1;

    public int PageSize { get; set; }

    /// <summary>
    /// Whether asking for the next page is worth it. Derived by over-fetching one row rather
    /// than a separate COUNT, so it stays accurate without a second query.
    /// </summary>
    public bool HasMore { get; set; }

    /// <summary>
    /// Total matches where the source can tell us cheaply; null when it cannot, in which case
    /// the client should show a count of what it has loaded rather than a total.
    /// </summary>
    public int? TotalAvailable { get; set; }

    public static PagedResult<T> Empty(int page, int pageSize) =>
        new() { Page = page, PageSize = pageSize, HasMore = false, Items = [] };
}
