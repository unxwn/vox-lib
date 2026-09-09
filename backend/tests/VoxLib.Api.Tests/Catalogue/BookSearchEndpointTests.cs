using System.Net;
using System.Net.Http.Json;
using VoxLib.Api.Tests.Infrastructure;

namespace VoxLib.Api.Tests.Catalogue;

/// <summary>
/// Finding a book by name. FR-013 and FR-015: the match is against titles and
/// author names, it ignores letter case, and it never reaches a book a visitor
/// is not allowed to see.
/// </summary>
[Collection(CatalogueCollection.Name)]
public class BookSearchEndpointTests(CatalogueApiFixture fixture)
{
    private readonly HttpClient _client = fixture.CreateClient();

    [Fact]
    public async Task Part_of_a_title_finds_the_books_that_carry_it()
    {
        var slugs = await SearchSlugsAsync("сон");

        // Both books are called "Сон", by different authors.
        Assert.Equal(["son-shevchenko", "son-vovchok"], slugs);
    }

    [Fact]
    public async Task Part_of_an_author_name_finds_everything_they_wrote()
    {
        var slugs = await SearchSlugsAsync("шевченко");

        Assert.Equal(["haidamaky", "yeretyk", "son-shevchenko"], slugs);
    }

    /// <summary>
    /// FR-015. A visitor typing on a phone gets whatever case the keyboard
    /// offers, so all three of these have to be the same search. This is the
    /// behaviour the deterministic collation plus ILIKE was chosen for, so it is
    /// worth asserting on the real database rather than assuming.
    /// </summary>
    [Theory]
    [InlineData("сон")]
    [InlineData("СОН")]
    [InlineData("Сон")]
    [InlineData("сОн")]
    public async Task Letter_case_makes_no_difference_to_a_Ukrainian_term(string term)
    {
        var slugs = await SearchSlugsAsync(term);

        Assert.Equal(["son-shevchenko", "son-vovchok"], slugs);
    }

    [Theory]
    [InlineData("kobzar")]
    [InlineData("KOBZAR")]
    [InlineData("Shevchenko")]
    public async Task Letter_case_makes_no_difference_to_a_Latin_term(string term)
    {
        var slugs = await SearchSlugsAsync(term);

        Assert.Contains("kobzar-selected-poems", slugs);
    }

    [Fact]
    public async Task A_search_never_reaches_a_book_that_is_not_published()
    {
        // Чорна рада is seeded as a draft. Neither its title nor its author may
        // find it, or the catalogue would confirm that an unpublished book
        // exists to anyone who guessed at the name.
        Assert.Empty(await SearchSlugsAsync("чорна"));
        Assert.Empty(await SearchSlugsAsync("рада"));
        Assert.Empty(await SearchSlugsAsync("куліш"));
    }

    [Fact]
    public async Task A_term_matching_a_title_and_a_term_matching_an_author_both_work_the_same_way()
    {
        var byTitle = await SearchAsync("інтермеццо");
        var byAuthor = await SearchAsync("коцюбинський");

        Assert.Single(byTitle.Items);
        Assert.Equal(3, byAuthor.Items.Count);

        // The count is the whole result set rather than the current page, which
        // is what the interface announces to a visitor.
        Assert.Equal(1, byTitle.TotalCount);
        Assert.Equal(3, byAuthor.TotalCount);
    }

    [Fact]
    public async Task Results_keep_the_catalogue_ordering()
    {
        var slugs = await SearchSlugsAsync("іван");

        // The same total order as the plain listing: by title under Ukrainian
        // collation, settled by slug.
        var expected = SeededCatalogue.SlugsInOrder.Where(slugs.Contains);

        Assert.Equal(expected, slugs);
    }

    /// <summary>
    /// A per-cent sign is a wildcard to the pattern matching underneath, so a
    /// term containing one would otherwise match every book in the catalogue
    /// rather than the nothing it should. FR-013 asks for text matching, not for
    /// a pattern language the visitor did not know they were writing.
    /// </summary>
    [Theory]
    [InlineData("%")]
    [InlineData("_")]
    [InlineData("со%н")]
    [InlineData("со_")]
    public async Task Pattern_characters_in_a_term_are_matched_literally(string term)
    {
        var page = await SearchAsync(term);

        Assert.Empty(page.Items);
        Assert.Equal(0, page.TotalCount);
    }

    private async Task<List<string>> SearchSlugsAsync(string term)
    {
        var page = await SearchAsync(term);

        return [.. page.Items.Select(book => book.Slug)];
    }

    private async Task<PagedBooksResponse> SearchAsync(string term, int page = 1)
    {
        var response = await _client.GetAsync(
            $"/api/books?q={Uri.EscapeDataString(term)}&page={page}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return await response.Content.ReadFromJsonAsync<PagedBooksResponse>()
            ?? throw new InvalidOperationException($"Searching for '{term}' returned no body.");
    }
}
