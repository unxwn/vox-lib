using VoxLib.Model.Common;

namespace VoxLib.Model.Book;

/// <summary>
/// The catalogue as the endpoints see it. Implemented in VoxLib.Orchestrator,
/// which owns the paging bounds, the published-only rule and the ordering, so
/// that no endpoint handler carries a business rule.
/// </summary>
public interface IBookCatalogue
{
    /// <summary>
    /// One page of the catalogue, or of the books matching <paramref name="term"/>
    /// when one is given. Null means the page does not exist: below the first
    /// page, or above the last page of a set that is not empty, per FR-020. Page
    /// one of an empty catalogue is a real page and comes back empty.
    /// </summary>
    Task<PagedResult<Book>?> ListAsync(
        int page,
        string? term,
        CancellationToken cancellationToken);

    /// <summary>
    /// One book by slug, or null when it does not exist or is not listed. The
    /// two are indistinguishable to the caller by design.
    /// </summary>
    Task<Book?> FindAsync(string slug, CancellationToken cancellationToken);
}
