using System.Net;
using System.Net.Http.Json;
using VoxLib.Api.Tests.Infrastructure;

namespace VoxLib.Api.Tests.Catalogue;

/// <summary>
/// What a book's own page is given to work with: its title, who wrote it, what
/// it is about, how it is divided and how long that comes to. FR-008.
/// </summary>
[Collection(SampleCatalogueCollection.Name)]
public class BookDetailEndpointTests(SampleCatalogueApiFixture fixture)
{
    private readonly HttpClient _client = fixture.CreateClient();

    [Fact]
    public async Task A_book_carries_every_piece_of_metadata_its_page_shows()
    {
        var book = await GetAsync("khiba-revut-voly");

        Assert.Equal("khiba-revut-voly", book.Slug);
        Assert.Equal("Хіба ревуть воли, як ясла повні?", book.Title);
        Assert.Equal(
            ["Іван Білик", "Панас Мирний"],
            book.Authors.Select(author => author.Name));
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

    /// <summary>
    /// The cover is a set of prepared widths rather than one URL, composed from
    /// configuration rather than stored. The widths are what a browser picks
    /// between; nothing is resized on request (FR-045).
    /// </summary>
    [Fact]
    public async Task A_book_with_cover_art_carries_every_prepared_width()
    {
        var book = await GetAsync(SampleCatalogue.SlugWithCoverArt);

        Assert.NotNull(book.Cover);
        Assert.Equal([160, 320], book.Cover.Sources.Select(source => source.Width));

        Assert.All(book.Cover.Sources, source =>
        {
            // Absolute and public: object storage serves these, not this API,
            // so a relative path would resolve against the wrong origin.
            Assert.True(
                Uri.TryCreate(source.Url, UriKind.Absolute, out _),
                $"'{source.Url}' is not an absolute address.");
            Assert.Contains(SampleCatalogue.SlugWithCoverArt, source.Url, StringComparison.Ordinal);
            Assert.EndsWith(".webp", source.Url, StringComparison.Ordinal);
        });
    }

    /// <summary>
    /// A book with no cover carries none at all rather than an empty set, which
    /// is what lets the interface tell "no cover" from "a cover with no widths".
    /// </summary>
    [Fact]
    public async Task A_book_without_cover_art_carries_no_cover_at_all()
    {
        var book = await GetAsync(SampleCatalogue.SlugWithoutCoverArt);

        Assert.Null(book.Cover);
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
        Assert.Equal(
            summary.Authors.Select(author => author.Slug),
            detail.Authors.Select(author => author.Slug));
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
