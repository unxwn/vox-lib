using VoxLib.Api.Book.Contracts.Responses;
using VoxLib.Model.Book;
using VoxLib.Model.Common;
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
                        : Results.Ok(ToPagedBooks(result));
                })
            .WithName("ListBooks")
            .WithSummary("List published books, ordered by title, optionally matching a term")
            .Produces<PagedBooks>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        routes
            .MapGet(
                "/api/books/{slug}",
                async (string slug, IBookCatalogue catalogue, CancellationToken cancellationToken) =>
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
                        : Results.Ok(ToDetail(book));
                })
            .WithName("GetBook")
            .WithSummary("Read one published book with its chapters")
            .Produces<BookDetail>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return routes;
    }

    private static PagedBooks ToPagedBooks(PagedResult<DomainBook> page) =>
        new(
            [.. page.Items.Select(ToSummary)],
            page.Page,
            page.PageSize,
            page.TotalCount,
            page.PageCount);

    private static BookDetail ToDetail(DomainBook book) =>
        new(
            book.Slug,
            book.Title,
            [.. book.Authors.Select(author => author.Name)],
            book.Description,
            book.CoverArtUrl,
            book.Language,
            book.ChapterCount,
            (int)book.TotalRunningTime.TotalSeconds,
            [.. book.Chapters.Select(ToChapter)]);

    private static ResponseChapter ToChapter(DomainChapter chapter) =>
        new(chapter.Position, chapter.Title, (int)chapter.RunningTime.TotalSeconds);

    private static BookSummary ToSummary(DomainBook book) =>
        new(
            book.Slug,
            book.Title,
            [.. book.Authors.Select(author => author.Name)],
            book.CoverArtUrl,
            book.ChapterCount,
            (int)book.TotalRunningTime.TotalSeconds);
}
