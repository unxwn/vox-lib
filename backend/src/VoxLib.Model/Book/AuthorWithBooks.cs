namespace VoxLib.Model.Book;

/// <summary>
/// A person and the published books they are credited on.
/// <para>
/// The roles are not on this record and are deliberately absent: how someone is
/// credited belongs to a pairing, and each book here already carries its own
/// credits. Repeating them would be two statements of one fact that can
/// disagree. A caller wanting "every role this person holds" derives it from
/// the books, which is what <see cref="Roles"/> does.
/// </para>
/// </summary>
/// <param name="Author">Who they are.</param>
/// <param name="Books">
/// Every published book they are credited on, in the catalogue's own title
/// order. Never empty: an author with none is not returned at all.
/// </param>
public sealed record AuthorWithBooks(Author Author, IReadOnlyList<Book> Books)
{
    /// <summary>
    /// Every distinct role this person holds across their published books.
    /// <para>
    /// A set rather than one value, because one person can be an author on one
    /// book and a compiler on another, and an index claiming a single role for
    /// them would be wrong about one of the two (FR-035).
    /// </para>
    /// </summary>
    public IReadOnlyList<CreditRole> Roles =>
    [
        .. Books
            .SelectMany(book => book.Authors)
            .Where(credit => credit.Author.Id == Author.Id)
            .Select(credit => credit.Role)
            .Distinct()
            .Order(),
    ];
}
