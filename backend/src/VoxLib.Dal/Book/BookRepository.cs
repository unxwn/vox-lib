using Microsoft.EntityFrameworkCore;
using VoxLib.Dal.Persistence;
using VoxLib.Model.Book;
using VoxLib.Model.Common;
using DomainBook = VoxLib.Model.Book.Book;

namespace VoxLib.Dal.Book;

/// <summary>
/// Reads the catalogue out of PostgreSQL and maps it to the domain. This is the
/// only place data access objects are turned into entities.
/// </summary>
public sealed class BookRepository(VoxLibDbContext database) : IBookRepository
{
    /// <summary>
    /// The escape character for a LIKE pattern. Backslash is PostgreSQL's
    /// default, named here because the pattern below escapes with it explicitly.
    /// </summary>
    private const string PatternEscape = "\\";

    public Task<PagedResult<DomainBook>> ListAsync(
        PageRequest page,
        CancellationToken cancellationToken) =>
        ReadPageAsync(Published(), page, cancellationToken);

    public Task<PagedResult<DomainBook>> SearchAsync(
        string term,
        PageRequest page,
        CancellationToken cancellationToken)
    {
        var pattern = $"%{EscapePattern(term)}%";

        // ILIKE rather than a lowercased comparison: it is the database's own
        // case-insensitive match, so it works for Cyrillic without the
        // application having to know how Ukrainian folds case. The column's
        // collation is deterministic precisely so this stays available.
        var matching = Published()
            .Where(book =>
                EF.Functions.ILike(book.Title, pattern, PatternEscape)
                || book.Authors.Any(author =>
                    EF.Functions.ILike(author.Name, pattern, PatternEscape)));

        return ReadPageAsync(matching, page, cancellationToken);
    }

    public async Task<DomainBook?> FindBySlugAsync(
        string slug,
        CancellationToken cancellationToken)
    {
        var book = await database
            .Books.Where(book =>
                book.Slug == slug && book.PublicationState == PublicationState.Published)
            .Include(book => book.Authors)
            .Include(book => book.Chapters)
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);

        // The published filter is in the query rather than applied to the result,
        // so a draft never leaves the database at all. Chapters and authors are
        // put in order by the mapping, which is the one place that decision is
        // made for both the listing and this read.
        return book?.ToDomain();
    }

    private IQueryable<BookDao> Published() =>
        database.Books.Where(book => book.PublicationState == PublicationState.Published);

    /// <summary>
    /// Turns a visitor's term into a literal one.
    /// <para>
    /// A per-cent sign and an underscore are wildcards to LIKE, so a term
    /// containing either would quietly stop being the text the visitor typed:
    /// searching for "%" would match the entire catalogue. FR-013 asks for text
    /// matching, not for a pattern language nobody knew they were writing.
    /// </para>
    /// </summary>
    private static string EscapePattern(string term) =>
        term.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);

    /// <summary>
    /// One page of whichever set of books it is given, ordered the one way the
    /// catalogue is ever ordered.
    /// <para>
    /// Shared by the listing and the search rather than written twice, because
    /// the two must agree: the order is by title under the column's Ukrainian
    /// collation and then by slug, and the slug is not decoration. Without it
    /// two books sharing a title are tied, the database may return them in
    /// either order, and paging can then show one twice or not at all.
    /// </para>
    /// </summary>
    private static async Task<PagedResult<DomainBook>> ReadPageAsync(
        IQueryable<BookDao> books,
        PageRequest page,
        CancellationToken cancellationToken)
    {
        var totalCount = await books.CountAsync(cancellationToken);

        var found = await books
            .OrderBy(book => book.Title)
            .ThenBy(book => book.Slug)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Include(book => book.Authors)
            .Include(book => book.Chapters)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return new PagedResult<DomainBook>(
            [.. found.Select(book => book.ToDomain())],
            page.Page,
            page.PageSize,
            totalCount);
    }
}
