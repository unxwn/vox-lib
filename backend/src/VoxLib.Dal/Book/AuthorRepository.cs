using Microsoft.EntityFrameworkCore;
using VoxLib.Dal.Persistence;
using VoxLib.Model.Book;

namespace VoxLib.Dal.Book;

/// <summary>
/// Reads authors out of PostgreSQL. Every query is filtered to people credited
/// on at least one published book, in the query rather than on the result, so a
/// draft-only author never leaves the database at all.
/// <para>
/// The people and their books are read as two queries rather than one. Walking
/// author to credit to book and back to credit is a cycle, which EF refuses in
/// a no-tracking query and would otherwise be a needless nesting of joins; and
/// the books have to carry their own full credits anyway, because a book on one
/// person's page still names its co-authors.
/// </para>
/// </summary>
public sealed class AuthorRepository(VoxLibDbContext database) : IAuthorRepository
{
    public async Task<IReadOnlyList<string>> SampleNamesAsync(
        int count,
        CancellationToken cancellationToken)
    {
        if (count <= 0)
        {
            return [];
        }

        // Ordered at random in the database rather than by reading every author
        // and shuffling here, so the sample costs one query and stays one query
        // as the catalogue grows.
        return await Credited()
            .OrderBy(_ => EF.Functions.Random())
            .Take(count)
            .Select(author => author.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuthorWithBooks>> ListAsync(
        CancellationToken cancellationToken)
    {
        var authors = await Credited()
            // sort_name carries the Ukrainian collation, which is what puts Ї
            // after Я rather than at the end of the alphabet. Settled by slug so
            // two people sorting identically still come back in one fixed order.
            .OrderBy(author => author.SortName)
            .ThenBy(author => author.Slug)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return await WithBooksAsync(authors, cancellationToken);
    }

    public async Task<AuthorWithBooks?> FindBySlugAsync(
        string slug,
        CancellationToken cancellationToken)
    {
        var author = await Credited()
            .Where(author => author.Slug == slug)
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);

        if (author is null)
        {
            return null;
        }

        var found = await WithBooksAsync([author], cancellationToken);

        return found.SingleOrDefault();
    }

    /// <summary>
    /// Everyone credited on at least one published book. An author with none is
    /// absent from the index and not found at their own address alike, which is
    /// FR-037: a name that leads nowhere is worse than no name.
    /// </summary>
    private IQueryable<AuthorDao> Credited() =>
        database.Authors.Where(author =>
            author.Credits.Any(credit =>
                credit.Book!.PublicationState == PublicationState.Published));

    /// <summary>
    /// The published books each of these people is credited on, in the
    /// catalogue's own title order, read in one further query for all of them.
    /// </summary>
    private async Task<IReadOnlyList<AuthorWithBooks>> WithBooksAsync(
        IReadOnlyList<AuthorDao> authors,
        CancellationToken cancellationToken)
    {
        if (authors.Count == 0)
        {
            return [];
        }

        var authorIds = authors.Select(author => author.Id).ToHashSet();

        var books = await database
            .Books.Where(book =>
                book.PublicationState == PublicationState.Published
                && book.Credits.Any(credit => authorIds.Contains(credit.AuthorId)))
            .OrderBy(book => book.Title)
            .ThenBy(book => book.Slug)
            .Include(book => book.Credits)
            .ThenInclude(credit => credit.Author)
            .Include(book => book.Chapters)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var mapped = books.Select(book => (Dao: book, Domain: book.ToDomain())).ToList();

        return
        [
            .. authors.Select(author => new AuthorWithBooks(
                author.ToDomain(),
                [
                    .. mapped
                        .Where(entry =>
                            entry.Dao.Credits.Any(credit => credit.AuthorId == author.Id))
                        .Select(entry => entry.Domain),
                ])),
        ];
    }
}
