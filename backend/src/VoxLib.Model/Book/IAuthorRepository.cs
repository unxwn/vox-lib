namespace VoxLib.Model.Book;

/// <summary>
/// Reads authors out of storage. Implemented in VoxLib.Dal. Every query here is
/// restricted to people credited on at least one published book, so an author
/// whose only book is a draft behaves exactly as if they did not exist —
/// the same rule the book repository follows, for the same reason.
/// </summary>
public interface IAuthorRepository
{
    /// <summary>
    /// A small random sample of names, for the search suggestions. Published
    /// books only, and the names are returned verbatim: FR-023's guarantee that
    /// activating a suggestion finds something holds only because nothing edits
    /// the string on the way out.
    /// </summary>
    Task<IReadOnlyList<string>> SampleNamesAsync(int count, CancellationToken cancellationToken);

    /// <summary>
    /// Everyone credited on at least one published book, ordered by sort name
    /// under the Ukrainian collation and settled by slug so the order is total
    /// and stable between requests.
    /// </summary>
    Task<IReadOnlyList<AuthorWithBooks>> ListAsync(CancellationToken cancellationToken);

    /// <summary>
    /// One author by slug with every published book they are credited on, or
    /// null when no such author exists or they have no published books. The two
    /// are indistinguishable to the caller by design.
    /// </summary>
    Task<AuthorWithBooks?> FindBySlugAsync(string slug, CancellationToken cancellationToken);
}
