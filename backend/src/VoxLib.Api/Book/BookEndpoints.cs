using VoxLib.Api.Author.Contracts.Responses;
using VoxLib.Api.Book.Contracts.Responses;
using VoxLib.Model.Book;
using VoxLib.Model.Common;
using VoxLib.Model.Storage;
using DomainBook = VoxLib.Model.Book.Book;
using DomainChapter = VoxLib.Model.Book.Chapter;
// Both namespaces have a Chapter: one is the entity, one is the wire shape.
using ResponseChapter = VoxLib.Api.Book.Contracts.Responses.Chapter;

namespace VoxLib.Api.Book;

/// <summary>
/// The catalogue's HTTP surface. The handlers parse the request, hand it to the
/// catalogue and shape the answer. No rule about which books exist, what order
/// they come in, or which pages are valid lives here.
/// </summary>
public static class BookEndpoints
{
    /// <summary>
    /// The longest search term accepted, matching contracts/catalogue.yaml. A
    /// term longer than this is a mistake or an attack rather than a search, and
    /// the bound is checked here because it is a fact about the request's shape,
    /// not a rule about the catalogue.
    /// </summary>
    public const int MaxSearchTermLength = 100;

    public static IEndpointRouteBuilder MapBookEndpoints(this IEndpointRouteBuilder routes)
    {
        routes
            .MapGet(
                "/api/books",
                async (
                    IBookCatalogue catalogue,
                    ICoverStorage covers,
                    CancellationToken cancellationToken,
                    int page = 1,
                    string? q = null) =>
                {
                    if (q is { Length: > MaxSearchTermLength })
                    {
                        return Results.Problem(
                            title: "Search term too long",
                            detail:
                                $"A search term may be at most {MaxSearchTermLength} characters.",
                            statusCode: StatusCodes.Status400BadRequest);
                    }

                    var result = await catalogue.ListAsync(page, q, cancellationToken);

                    return result is null
                        ? Results.Problem(
                            title: "No such page",
                            detail: $"The catalogue has no page {page}.",
                            statusCode: StatusCodes.Status404NotFound)
                        : Results.Ok(ToPagedBooks(result, covers));
                })
            .WithName("ListBooks")
            .WithSummary("List published books, ordered by title, optionally matching a term")
            .Produces<PagedBooks>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        routes
            .MapGet(
                "/api/books/{slug}",
                async (
                    string slug,
                    IBookCatalogue catalogue,
                    ICoverStorage covers,
                    CancellationToken cancellationToken) =>
                {
                    var book = await catalogue.FindAsync(slug, cancellationToken);

                    // One answer for a book that was never here and for one that
                    // is not published yet. The handler cannot tell them apart
                    // either, which is the point: the catalogue does not disclose
                    // that a book is on its way.
                    return book is null
                        ? Results.Problem(
                            title: "No such book",
                            detail: $"The catalogue has no book '{slug}'.",
                            statusCode: StatusCodes.Status404NotFound)
                        : Results.Ok(ToDetail(book, covers));
                })
            .WithName("GetBook")
            .WithSummary("Read one published book with its chapters")
            .Produces<BookDetail>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return routes;
    }

    private static PagedBooks ToPagedBooks(
        PagedResult<DomainBook> page,
        ICoverStorage covers) =>
        new(
            [.. page.Items.Select(book => ToSummary(book, covers))],
            page.Page,
            page.PageSize,
            page.TotalCount,
            page.PageCount);

    private static BookDetail ToDetail(DomainBook book, ICoverStorage covers) =>
        new(
            book.Slug,
            book.Title,
            [.. book.Authors.Select(ToReference)],
            book.Description,
            ToCover(book, covers),
            book.Narrator,
            book.Language,
            book.ChapterCount,
            (int)book.TotalRunningTime.TotalSeconds,
            [.. book.Chapters.Select(ToChapter)]);

    private static ResponseChapter ToChapter(DomainChapter chapter) =>
        new(chapter.Position, chapter.Title, (int)chapter.RunningTime.TotalSeconds);

    /// <summary>
    /// Public because the author resource returns the same shape for the books
    /// on a person's page. One conversion rather than two that can disagree
    /// about what a book looks like in a list.
    /// </summary>
    public static BookSummary ToSummary(DomainBook book, ICoverStorage covers) =>
        new(
            book.Slug,
            book.Title,
            [.. book.Authors.Select(ToReference)],
            ToCover(book, covers),
            book.ChapterCount,
            (int)book.TotalRunningTime.TotalSeconds);

    /// <summary>
    /// The prepared widths for a book that has a cover, and null for one that
    /// does not — which is a designed state rather than a missing value, and is
    /// what the placeholder is for.
    /// <para>
    /// Composing a URL touches no network, so this cannot fail and a catalogue
    /// response never depends on storage being reachable (FR-051). Whether the
    /// object is actually there is the browser's problem, and a failed image is
    /// swapped for the same placeholder.
    /// </para>
    /// </summary>
    private static Cover? ToCover(DomainBook book, ICoverStorage covers) =>
        book.CoverKey is { Length: > 0 } key
            ? new Cover(
                [
                    .. CoverWidths.Prepared.Select(width =>
                        new CoverSource(covers.PublicUrl(key, width).ToString(), width)),
                ])
            : null;

    private static AuthorReference ToReference(AuthorCredit credit) =>
        new(credit.Author.Slug, credit.Author.Name, credit.Role);
}
