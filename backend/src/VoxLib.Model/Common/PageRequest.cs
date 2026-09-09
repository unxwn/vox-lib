namespace VoxLib.Model.Common;

/// <summary>
/// One valid, one-based page of a listing. Page size is fixed in this feature
/// and is deliberately not a caller-supplied value.
/// </summary>
public sealed class PageRequest
{
    public const int FixedPageSize = 20;

    private PageRequest(int page) => Page = page;

    public int Page { get; }

    public int PageSize => FixedPageSize;

    /// <summary>How many books precede this page.</summary>
    public int Skip => (Page - 1) * FixedPageSize;

    /// <summary>
    /// Returns null for a page number below the first page. Rejecting it here
    /// rather than clamping is what lets the caller answer FR-020 with a
    /// not-found rather than quietly serving page one.
    /// </summary>
    public static PageRequest? TryCreate(int page) => page < 1 ? null : new PageRequest(page);
}
