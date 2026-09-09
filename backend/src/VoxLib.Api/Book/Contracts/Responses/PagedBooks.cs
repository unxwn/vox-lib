namespace VoxLib.Api.Book.Contracts.Responses;

/// <summary>
/// One page of the catalogue. Carries enough for a visitor to be told which page
/// they are on, how many there are, and which books this page covers, as FR-004
/// requires.
/// </summary>
/// <param name="TotalCount">Published books matching the request, across all pages.</param>
/// <param name="PageCount">Zero when nothing matches.</param>
public sealed record PagedBooks(
    IReadOnlyList<BookSummary> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int PageCount);
