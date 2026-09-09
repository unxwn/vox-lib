using System.Net;
using System.Net.Http.Json;
using VoxLib.Api.Tests.Infrastructure;

namespace VoxLib.Api.Tests.Catalogue;

/// <summary>
/// Where the pages end. FR-020 draws the line: a page that does not exist is not
/// found, and an empty catalogue still has a page one to put the empty state on.
/// </summary>
[Collection(CatalogueCollection.Name)]
public class BookListPagingTests(CatalogueApiFixture fixture)
{
    private readonly HttpClient _client = fixture.CreateClient();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task A_page_below_the_first_is_not_found(int page)
    {
        var response = await _client.GetAsync($"/api/books?page={page}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertProblemDetailsAsync(response);
    }

    [Fact]
    public async Task A_page_beyond_the_last_is_not_found_rather_than_empty()
    {
        var response = await _client.GetAsync("/api/books?page=9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertProblemDetailsAsync(response);
    }

    /// <summary>
    /// Distinct from a page that does not exist. "abc" is not a page number at
    /// all, which is the caller's mistake and not a server fault.
    /// </summary>
    [Fact]
    public async Task A_page_number_that_is_not_a_number_is_a_bad_request()
    {
        var response = await _client.GetAsync("/api/books?page=abc");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task The_last_page_is_still_found()
    {
        var response = await _client.GetAsync($"/api/books?page={SeededCatalogue.PageCount}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Two books share a title, and the seed places them either side of the page
    /// boundary on purpose. Sorted by title alone the database is free to return
    /// them in either order, so one could appear on both pages or on neither.
    /// </summary>
    [Fact]
    public async Task Paging_across_two_books_with_the_same_title_neither_repeats_nor_skips()
    {
        var first = await GetPageAsync("/api/books?page=1");
        var second = await GetPageAsync("/api/books?page=2");

        var titles = first
            .Items.Concat(second.Items)
            .Where(book => book.Title == SeededCatalogue.TitleSharedByTwoBooks)
            .Select(book => book.Slug)
            .ToList();

        Assert.Equal(2, titles.Count);
        Assert.Equal(SeededCatalogue.LastSlugOnFirstPage, first.Items[^1].Slug);
        Assert.Equal(SeededCatalogue.FirstSlugOnSecondPage, second.Items[0].Slug);
    }

    [Fact]
    public async Task Every_book_appears_exactly_once_across_the_pages()
    {
        var slugs = new List<string>();

        foreach (var pageNumber in Enumerable.Range(1, SeededCatalogue.PageCount))
        {
            var page = await GetPageAsync($"/api/books?page={pageNumber}");
            slugs.AddRange(page.Items.Select(book => book.Slug));
        }

        Assert.Equal(SeededCatalogue.PublishedCount, slugs.Count);
        Assert.Equal(SeededCatalogue.PublishedCount, slugs.Distinct().Count());
    }

    /// <summary>
    /// Repeating the same request has to give the same page. If the order were
    /// only partial the database could legally shuffle the tied rows between two
    /// identical requests.
    /// </summary>
    [Fact]
    public async Task The_same_page_requested_twice_is_the_same_page()
    {
        var first = await GetPageAsync("/api/books?page=1");
        var again = await GetPageAsync("/api/books?page=1");

        Assert.Equal(
            first.Items.Select(book => book.Slug),
            again.Items.Select(book => book.Slug));
    }

    private async Task<PagedBooksResponse> GetPageAsync(string path)
    {
        var response = await _client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return await response.Content.ReadFromJsonAsync<PagedBooksResponse>()
            ?? throw new InvalidOperationException($"'{path}' returned no body.");
    }

    private static async Task AssertProblemDetailsAsync(HttpResponseMessage response)
    {
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.NotFound, problem.Status);
        Assert.False(string.IsNullOrWhiteSpace(problem.Title));
    }

    private sealed record ProblemDetailsResponse(string? Type, string? Title, int Status);
}

/// <summary>
/// An empty catalogue is not the same as a catalogue whose pages have run out.
/// Page one of it is a real page, and it carries the empty state FR-007 requires.
/// </summary>
[Collection(EmptyCatalogueCollection.Name)]
public class EmptyCatalogueListTests(EmptyCatalogueApiFixture fixture)
{
    private readonly HttpClient _client = fixture.CreateClient();

    [Fact]
    public async Task Page_one_of_an_empty_catalogue_is_found_and_empty()
    {
        var response = await _client.GetAsync("/api/books");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await response.Content.ReadFromJsonAsync<PagedBooksResponse>();

        Assert.NotNull(page);
        Assert.Empty(page.Items);
        Assert.Equal(1, page.Page);
        Assert.Equal(0, page.TotalCount);
        Assert.Equal(0, page.PageCount);
    }

    [Fact]
    public async Task Page_two_of_an_empty_catalogue_is_not_found()
    {
        var response = await _client.GetAsync("/api/books?page=2");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
