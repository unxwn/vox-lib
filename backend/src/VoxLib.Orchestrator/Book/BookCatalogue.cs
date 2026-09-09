using VoxLib.Model.Book;
using VoxLib.Model.Common;
using DomainBook = VoxLib.Model.Book.Book;

namespace VoxLib.Orchestrator.Book;

/// <summary>
/// The catalogue's rules: which books may be seen, in what order, and which
/// pages exist. Endpoint handlers parse and shape; the decisions are here.
/// </summary>
public sealed class BookCatalogue(IBookRepository books) : IBookCatalogue
{
    public async Task<PagedResult<DomainBook>?> ListAsync(
        int page,
        string? term,
        CancellationToken cancellationToken)
    {
        // A page number below the first page is not a page at all. Clamping it to
        // page one would invent a second address for the same content.
        if (PageRequest.TryCreate(page) is not { } request)
        {
            return null;
        }

        // An empty box, or one holding only spaces, is not a search for nothing:
        // it is the absence of a search. Treating it as no term is what lets the
        // interface handle clearing the box as a return to browsing, rather than
        // as a search that happened to match every book.
        var searchFor = term?.Trim();

        var result = string.IsNullOrEmpty(searchFor)
            ? await books.ListAsync(request, cancellationToken)
            : await books.SearchAsync(searchFor, request, cancellationToken);

        // Nothing found and a page past the end look alike from here and are not
        // the same thing. Page one is always a real page, carrying either the
        // empty catalogue or the "no results" state; a page past the end of a set
        // that does have contents is a mistake, and saying so keeps crawlers from
        // indexing an unbounded run of empty pages. FR-020, and it applies to a
        // result set exactly as it does to the catalogue.
        return result.Items.Count == 0 && request.Page > 1 ? null : result;
    }

    public async Task<DomainBook?> FindAsync(string slug, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        var book = await books.FindBySlugAsync(slug, cancellationToken);

        // Asked again on the way out, through the entity's own rule rather than
        // by repeating the condition. The repository already filters, so this is
        // belt and braces on the requirement that matters most in this feature:
        // an unpublished book must behave exactly as if it did not exist, and
        // "not listed" and "not there" reach the caller as the same answer.
        return book?.IsListed == true ? book : null;
    }
}
