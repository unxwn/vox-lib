using System.Net;
using System.Net.Http.Json;
using VoxLib.Api.Tests.Infrastructure;

namespace VoxLib.Api.Tests.Catalogue;

/// <summary>
/// What a search does when the term is not a sensible one, and what it does when
/// it matches more than fits on a page. These are the cases a visitor reaches by
/// accident, so each has to answer with something the interface can render.
/// </summary>
[Collection(CatalogueCollection.Name)]
public class BookSearchEdgeCaseTests(CatalogueApiFixture fixture)
{
    /// <summary>
    /// The bound from contracts/catalogue.yaml. A term longer than this is a
    /// mistake or an attack, not a search.
    /// </summary>
    private const int MaxTermLength = 100;

    private readonly HttpClient _client = fixture.CreateClient();

    /// <summary>
    /// An empty box is not a search for nothing, it is the absence of a search.
    /// Returning the whole catalogue is what lets the interface treat clearing
    /// the box as going back to browsing, rather than as a search that found
    /// every book by coincidence.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    public async Task An_empty_or_blank_term_lists_the_whole_catalogue(string term)
    {
        var page = await GetAsync($"/api/books?q={Uri.EscapeDataString(term)}");

        Assert.Equal(SeededCatalogue.PublishedCount, page.TotalCount);
        Assert.Equal(SeededCatalogue.PageSize, page.Items.Count);
        Assert.Equal(SeededCatalogue.PageCount, page.PageCount);
    }

    [Fact]
    public async Task A_term_is_trimmed_before_it_is_matched()
    {
        var padded = await GetAsync($"/api/books?q={Uri.EscapeDataString("  сон  ")}");

        Assert.Equal(2, padded.TotalCount);
    }

    [Fact]
    public async Task A_term_at_the_length_bound_is_still_a_search()
    {
        var term = new string('а', MaxTermLength);

        var response = await _client.GetAsync($"/api/books?q={term}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task A_term_above_the_length_bound_is_rejected_rather_than_matched()
    {
        var term = new string('а', MaxTermLength + 1);

        var response = await _client.GetAsync($"/api/books?q={term}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    /// <summary>
    /// Nothing found is a real answer and a valid page, not an error: the
    /// interface has an empty state to show for it, and a 404 here would make
    /// that unreachable.
    /// </summary>
    [Fact]
    public async Task A_term_matching_nothing_is_an_empty_page_rather_than_a_not_found()
    {
        var page = await GetAsync("/api/books?q=щосьчогонемає");

        Assert.Empty(page.Items);
        Assert.Equal(0, page.TotalCount);
        Assert.Equal(0, page.PageCount);
        Assert.Equal(1, page.Page);
    }

    [Fact]
    public async Task Results_that_do_not_fit_on_one_page_are_paged_like_the_catalogue()
    {
        // "а" appears in the title or the author of 23 published books, which is
        // more than one page holds.
        var first = await GetAsync("/api/books?q=а");

        Assert.Equal(23, first.TotalCount);
        Assert.Equal(2, first.PageCount);
        Assert.Equal(SeededCatalogue.PageSize, first.Items.Count);

        var second = await GetAsync("/api/books?q=а&page=2");

        Assert.Equal(3, second.Items.Count);
        Assert.Equal(23, second.TotalCount);

        // Paging a result set neither repeats a book nor loses one, for the same
        // reason the catalogue does not: the order is total.
        var all = first.Items.Concat(second.Items).Select(book => book.Slug).ToList();

        Assert.Equal(23, all.Count);
        Assert.Equal(all.Count, all.Distinct().Count());
    }

    [Fact]
    public async Task A_page_beyond_the_last_page_of_a_result_set_is_not_found()
    {
        // FR-020 applies to a result set exactly as it does to the catalogue, so
        // a crawler cannot walk off the end into an unbounded run of empty pages.
        var response = await _client.GetAsync("/api/books?q=сон&page=2");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task A_page_below_the_first_is_not_found_whether_or_not_a_term_is_given()
    {
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await _client.GetAsync("/api/books?q=сон&page=0")).StatusCode);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await _client.GetAsync("/api/books?q=сон&page=-1")).StatusCode);
    }

    private async Task<PagedBooksResponse> GetAsync(string path)
    {
        var response = await _client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return await response.Content.ReadFromJsonAsync<PagedBooksResponse>()
            ?? throw new InvalidOperationException($"'{path}' returned no body.");
    }
}
