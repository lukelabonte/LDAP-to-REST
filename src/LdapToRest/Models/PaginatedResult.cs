namespace LdapToRest.Models;

/// <summary>
/// A paginated list of results. Use `page` and `pageSize` query parameters to navigate.
/// </summary>
/// <typeparam name="T">The type of items in the list</typeparam>
public class PaginatedResult<T>
{
    /// <summary>The items on the current page</summary>
    public required List<T> Items { get; set; }

    /// <summary>Current page number (1-based)</summary>
    public int Page { get; set; }

    /// <summary>Number of items per page</summary>
    public int PageSize { get; set; }

    /// <summary>Total number of items across all pages</summary>
    public int TotalCount { get; set; }

    /// <summary>Total number of pages</summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>Whether there is a next page available</summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>Whether there is a previous page available</summary>
    public bool HasPreviousPage => Page > 1;
}
