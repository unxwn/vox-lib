using VoxLib.Model.Common;

namespace VoxLib.Model.Book;

/// <summary>
/// Reads published books. Implemented in VoxLib.Dal. Only published books are
/// ever returned, so an unpublished book behaves exactly as if it did not exist.
/// </summary>
public interface IBookRepository
{
    /// <summary>
    /// One page of published books, ordered by title under Ukrainian collation
    /// and settled by slug so the order is total and paging never repeats or
    /// skips a book.
    /// </summary>
    Task<PagedResult<Book>> ListAsync(PageRequest page, CancellationToken cancellationToken);

    /// <summary>
    /// One page of published books whose title or author name contains
    /// <paramref name="term"/>, matched without regard to letter case, in the
    /// same order as <see cref="ListAsync"/>.
    /// </summary>
    Task<PagedResult<Book>> SearchAsync(
        string term,
        PageRequest page,
        CancellationToken cancellationToken);

    /// <summary>
    /// The published book with this slug, with its chapters in position order,
    /// or null when no published book has it.
    /// </summary>
    Task<Book?> FindBySlugAsync(string slug, CancellationToken cancellationToken);
}
