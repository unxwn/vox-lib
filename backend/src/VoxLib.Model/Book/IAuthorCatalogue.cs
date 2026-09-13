namespace VoxLib.Model.Book;

/// <summary>
/// Authors as the endpoints see them. Implemented in VoxLib.Orchestrator, which
/// owns the published-only rule and the ordering, so that no endpoint handler
/// carries a business rule.
/// <para>
/// It sits beside <see cref="IBookCatalogue"/> rather than introducing a
/// pattern: authors are a resource in their own right (FR-036), not a filter on
/// books, so they get the shape books already have.
/// </para>
/// </summary>
public interface IAuthorCatalogue
{
    /// <summary>
    /// Every author with at least one published book, in index order. Unpaged:
    /// FR-035 asks for the whole index, and nothing in the catalogue is close to
    /// a page's worth.
    /// </summary>
    Task<IReadOnlyList<AuthorWithBooks>> ListAsync(CancellationToken cancellationToken);

    /// <summary>
    /// One author by slug, or null when they do not exist or have no published
    /// books. One answer for both, for the same reason an unpublished book is
    /// indistinguishable from an absent one.
    /// </summary>
    Task<AuthorWithBooks?> FindAsync(string slug, CancellationToken cancellationToken);
}
