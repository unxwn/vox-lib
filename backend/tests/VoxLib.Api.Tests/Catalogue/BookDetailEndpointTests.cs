using System.Net;
using System.Net.Http.Json;
using VoxLib.Api.Tests.Infrastructure;

namespace VoxLib.Api.Tests.Catalogue;

/// <summary>
/// What a book's own page is given to work with: its title, who wrote it, what
/// it is about, how it is divided and how long that comes to. FR-008.
/// </summary>
[Collection(CatalogueCollection.Name)]
public class BookDetailEndpointTests(CatalogueApiFixture fixture)
{
    private readonly HttpClient _client = fixture.CreateClient();

    [Fact]
    public async Task A_book_carries_every_piece_of_metadata_its_page_shows()
    {
        var book = await GetAsync("khiba-revut-voly");

        Assert.Equal("khiba-revut-voly", book.Slug);
        Assert.Equal("Хіба ревуть воли, як ясла повні?", book.Title);
        Assert.Equal(["Іван Білик", "Панас Мирний"], book.Authors);
        Assert.False(string.IsNullOrWhiteSpace(book.Description));
        Assert.Equal(4, book.ChapterCount);
    }

    [Fact]
    public async Task Chapters_arrive_in_position_order()
    {
        var book = await GetAsync("eneida");

        Assert.Equal([1, 2, 3, 4, 5], book.Chapters.Select(chapter => chapter.Position));

        // Ordered, not merely sorted after the fact: the positions have to be the
        // book's own, so a reader is told "part 3" for the third chapter.
        Assert.Equal(book.ChapterCount, book.Chapters.Count);
        Assert.All(book.Chapters, chapter => Assert.False(string.IsNullOrWhiteSpace(chapter.Title)));
    }

    [Fact]
    public async Task Total_running_time_is_the_sum_of_the_chapters()
    {
        var book = await GetAsync("khiba-revut-voly");

        Assert.Equal(
            book.Chapters.Sum(chapter => chapter.RunningTimeSeconds),
            book.TotalRunningTimeSeconds);
        Assert.Equal(3900 + 4080 + 3720 + 3540, book.TotalRunningTimeSeconds);
    }

    [Fact]
    public async Task A_book_with_cover_art_carries_its_address()
    {
        var book = await GetAsync("boiarynia");

        Assert.False(string.IsNullOrWhiteSpace(book.CoverArtUrl));
    }

    /// <summary>
    /// FR-018. A page cannot declare the language of its own content unless the
    /// API says what that language is, so the field is part of the contract
    /// rather than something the frontend guesses from the interface locale.
    /// </summary>
    [Theory]
    [InlineData("lisova-pisnia", "uk")]
    [InlineData("kobzar-selected-poems", "en")]
    public async Task A_book_states_the_language_its_metadata_is_written_in(
        string slug,
        string expected)
    {
        var book = await GetAsync(slug);

        Assert.Equal(expected, book.Language);
    }

    [Fact]
    public async Task The_detail_view_agrees_with_the_listing_about_the_same_book()
    {
        var detail = await GetAsync("haidamaky");

        var listing = await _client.GetFromJsonAsync<PagedBooksResponse>("/api/books")
            ?? throw new InvalidOperationException("The catalogue returned no body.");
        var summary = listing.Items.Single(book => book.Slug == "haidamaky");

        Assert.Equal(summary.Title, detail.Title);
        Assert.Equal(summary.Authors, detail.Authors);
        Assert.Equal(summary.ChapterCount, detail.ChapterCount);
        Assert.Equal(summary.TotalRunningTimeSeconds, detail.TotalRunningTimeSeconds);
    }

    private async Task<BookDetailResponse> GetAsync(string slug)
    {
        var response = await _client.GetAsync($"/api/books/{slug}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return await response.Content.ReadFromJsonAsync<BookDetailResponse>()
            ?? throw new InvalidOperationException($"'{slug}' returned no body.");
    }
}
