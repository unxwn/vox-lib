using System.Net;
using System.Net.Http.Json;
using VoxLib.Api.Tests.Infrastructure;

namespace VoxLib.Api.Tests.Catalogue;

/// <summary>
/// The listing itself: what order the books come in, how many arrive at a time,
/// and whether a visitor can tell where they are. FR-001, FR-004, FR-005, FR-015.
/// </summary>
[Collection(CatalogueCollection.Name)]
public class BookListEndpointTests(CatalogueApiFixture fixture)
{
    private readonly HttpClient _client = fixture.CreateClient();

    [Fact]
    public async Task First_page_carries_twenty_books_and_says_where_the_visitor_is()
    {
        var page = await GetPageAsync("/api/books");

        Assert.Equal(SeededCatalogue.PageSize, page.Items.Count);
        Assert.Equal(1, page.Page);
        Assert.Equal(SeededCatalogue.PageSize, page.PageSize);
        Assert.Equal(SeededCatalogue.PublishedCount, page.TotalCount);
        Assert.Equal(SeededCatalogue.PageCount, page.PageCount);
    }

    [Fact]
    public async Task Last_page_carries_the_remainder()
    {
        var page = await GetPageAsync("/api/books?page=2");

        Assert.Equal(SeededCatalogue.PublishedCount - SeededCatalogue.PageSize, page.Items.Count);
        Assert.Equal(2, page.Page);
        Assert.Equal(SeededCatalogue.PublishedCount, page.TotalCount);
    }

    [Fact]
    public async Task Books_are_ordered_by_title_under_Ukrainian_collation()
    {
        var slugs = await EveryPublishedSlugAsync();

        Assert.Equal(SeededCatalogue.SlugsInOrder, slugs);
    }

    /// <summary>
    /// Ґ, Є, І and Ї sit outside the contiguous Cyrillic run, so a byte ordering
    /// scatters them to the ends of the catalogue. Asserting their neighbours
    /// directly says what went wrong when the collation is lost, which comparing
    /// two long lists does not.
    /// </summary>
    [Theory]
    [InlineData("haidamaky", "gudzyk")] // Гайдамаки, then Ґудзик
    [InlineData("gudzyk", "dim-na-hori")] // Ґудзик, then Дім на горі
    [InlineData("eneida", "yeretyk")] // Енеїда, then Єретик
    [InlineData("zemlia", "intermezzo")] // Земля, then Інтермеццо
    [InlineData("intermezzo", "yizhachok-i-zymova-kazka")] // Інтермеццо, then Їжачок
    public async Task Ukrainian_letters_outside_the_main_run_sort_where_a_reader_expects(
        string before,
        string after)
    {
        var slugs = await EveryPublishedSlugAsync();

        Assert.Equal(slugs.IndexOf(before) + 1, slugs.IndexOf(after));
    }

    [Fact]
    public async Task A_book_that_is_not_published_is_absent_from_every_page()
    {
        var slugs = await EveryPublishedSlugAsync();

        Assert.DoesNotContain(SeededCatalogue.DraftSlug, slugs);
    }

    [Fact]
    public async Task A_book_without_cover_art_is_listed_with_none_rather_than_omitted()
    {
        var book = await FindAsync(SeededCatalogue.SlugWithoutCoverArt);

        Assert.Null(book.CoverArtUrl);
        Assert.NotEmpty(book.Title);
    }

    [Fact]
    public async Task A_book_without_chapters_runs_for_no_time_rather_than_reporting_nothing()
    {
        var book = await FindAsync(SeededCatalogue.SlugWithoutChapters);

        Assert.Equal(0, book.ChapterCount);
        Assert.Equal(0, book.TotalRunningTimeSeconds);
    }

    [Fact]
    public async Task A_summary_names_its_authors_and_totals_its_chapters()
    {
        var book = await FindAsync("khiba-revut-voly");

        Assert.Equal(["Іван Білик", "Панас Мирний"], book.Authors);
        Assert.Equal(4, book.ChapterCount);
        Assert.Equal(3900 + 4080 + 3720 + 3540, book.TotalRunningTimeSeconds);
    }

    private async Task<BookSummaryResponse> FindAsync(string slug)
    {
        foreach (var pageNumber in Enumerable.Range(1, SeededCatalogue.PageCount))
        {
            var page = await GetPageAsync($"/api/books?page={pageNumber}");
            var match = page.Items.FirstOrDefault(book => book.Slug == slug);

            if (match is not null)
            {
                return match;
            }
        }

        throw new InvalidOperationException($"'{slug}' is not listed anywhere in the catalogue.");
    }

    private async Task<List<string>> EveryPublishedSlugAsync()
    {
        var slugs = new List<string>();

        foreach (var pageNumber in Enumerable.Range(1, SeededCatalogue.PageCount))
        {
            var page = await GetPageAsync($"/api/books?page={pageNumber}");
            slugs.AddRange(page.Items.Select(book => book.Slug));
        }

        return slugs;
    }

    private async Task<PagedBooksResponse> GetPageAsync(string path)
    {
        var response = await _client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return await response.Content.ReadFromJsonAsync<PagedBooksResponse>()
            ?? throw new InvalidOperationException($"'{path}' returned no body.");
    }
}
