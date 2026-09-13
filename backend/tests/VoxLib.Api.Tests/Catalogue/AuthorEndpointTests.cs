using System.Net;
using System.Net.Http.Json;
using VoxLib.Api.Tests.Infrastructure;

namespace VoxLib.Api.Tests.Catalogue;

/// <summary>
/// SC-013: a name on a book reaches the person, and the person's page lists
/// exactly the books they are credited on.
/// </summary>
[Collection(SampleCatalogueCollection.Name)]
public class AuthorEndpointTests(SampleCatalogueApiFixture fixture)
{
    private readonly HttpClient _client = fixture.CreateClient();

    /// <summary>
    /// Every credit carries the slug it is reachable at, which is what lets a
    /// name be a link without a second request (FR-033). Asserted by following
    /// each one rather than by checking the string is non-empty.
    /// </summary>
    [Fact]
    public async Task Every_author_on_a_book_resolves_at_their_own_address()
    {
        var book = await _client.GetFromJsonAsync<BookDetailResponse>(
            $"/api/books/{SampleCatalogue.SlugWithTwoAuthors}")
            ?? throw new InvalidOperationException("The book returned no body.");

        Assert.NotEmpty(book.Authors);

        foreach (var author in book.Authors)
        {
            var response = await _client.GetAsync($"/api/authors/{author.Slug}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task Every_credit_says_how_the_person_is_credited()
    {
        var book = await _client.GetFromJsonAsync<BookDetailResponse>(
            $"/api/books/{SampleCatalogue.SlugWithTwoAuthors}")
            ?? throw new InvalidOperationException("The book returned no body.");

        Assert.All(book.Authors, author =>
            Assert.Contains(author.Role, new[] { "author", "compiler" }));
    }

    /// <summary>
    /// The author's page against a direct query of what they are credited on.
    /// Comparing the two is what makes "exactly the books they are on" checkable
    /// rather than a count that happens to agree.
    /// </summary>
    [Fact]
    public async Task An_author_page_lists_exactly_the_books_they_are_credited_on()
    {
        var index = await IndexAsync();
        var entry = index.Items.First();

        var author = await _client.GetFromJsonAsync<AuthorDetailResponse>(
            $"/api/authors/{entry.Slug}")
            ?? throw new InvalidOperationException("The author returned no body.");

        var fromTheCatalogue = new List<string>();

        foreach (var page in Enumerable.Range(1, SampleCatalogue.PageCount))
        {
            var books = await _client.GetFromJsonAsync<PagedBooksResponse>($"/api/books?page={page}")
                ?? throw new InvalidOperationException("The catalogue returned no body.");

            fromTheCatalogue.AddRange(
                books
                    .Items.Where(book =>
                        book.Authors.Any(credit => credit.Slug == entry.Slug))
                    .Select(book => book.Slug));
        }

        Assert.Equal(
            fromTheCatalogue.Order(),
            author.Books.Select(book => book.Slug).Order());
        Assert.Equal(author.BookCount, author.Books.Count);
    }

    [Fact]
    public async Task An_author_page_never_lists_a_book_that_is_not_published()
    {
        var index = await IndexAsync();

        foreach (var entry in index.Items)
        {
            var author = await _client.GetFromJsonAsync<AuthorDetailResponse>(
                $"/api/authors/{entry.Slug}")
                ?? throw new InvalidOperationException("The author returned no body.");

            Assert.DoesNotContain(
                SampleCatalogue.DraftSlug,
                author.Books.Select(book => book.Slug));
        }
    }

    private async Task<AuthorIndexResponse> IndexAsync() =>
        await _client.GetFromJsonAsync<AuthorIndexResponse>("/api/authors")
        ?? throw new InvalidOperationException("The index returned no body.");
}
