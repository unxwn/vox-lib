using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using VoxLib.Api.Tests.Infrastructure;

namespace VoxLib.Api.Tests.Catalogue;

/// <summary>
/// The two ways a book can fail to be there, and the several ways it can be
/// sparse without being absent. FR-011.
/// </summary>
[Collection(CatalogueCollection.Name)]
public partial class BookDetailNotFoundTests(CatalogueApiFixture fixture)
{
    private readonly HttpClient _client = fixture.CreateClient();

    [Theory]
    [InlineData("no-such-book-at-all")]
    [InlineData(SeededCatalogue.DraftSlug)]
    public async Task A_book_that_may_not_be_read_is_not_found(string slug)
    {
        var response = await _client.GetAsync($"/api/books/{slug}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    /// <summary>
    /// An unpublished book and one that never existed must answer alike. If the
    /// draft answered differently, the catalogue would confirm that a book is
    /// coming, which is not the API's to disclose. Comparing the two bodies with
    /// the slug itself blanked out is what makes "alike" checkable, rather than
    /// asserting on a status code that both share for unrelated reasons.
    /// </summary>
    [Fact]
    public async Task An_unpublished_book_is_indistinguishable_from_one_that_never_existed()
    {
        var draft = await ComparableNotFoundBodyAsync(SeededCatalogue.DraftSlug);
        var absent = await ComparableNotFoundBodyAsync("no-such-book-at-all");

        Assert.Equal(absent, draft);
    }

    [Fact]
    public async Task A_draft_book_is_absent_from_the_listing_as_well_as_from_its_own_address()
    {
        var listing = await _client.GetFromJsonAsync<PagedBooksResponse>("/api/books?page=2")
            ?? throw new InvalidOperationException("The catalogue returned no body.");

        Assert.DoesNotContain(SeededCatalogue.DraftSlug, listing.Items.Select(book => book.Slug));
    }

    /// <summary>
    /// Sparse is not the same as missing. Each of these books is published and
    /// readable, and its page has to render from what it does have.
    /// </summary>
    [Theory]
    [InlineData(SeededCatalogue.SlugWithoutChapters)]
    [InlineData(SeededCatalogue.SlugWithoutDescription)]
    [InlineData(SeededCatalogue.SlugWithoutCoverArt)]
    public async Task A_published_book_missing_optional_metadata_is_still_readable(string slug)
    {
        var response = await _client.GetAsync($"/api/books/{slug}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var book = await response.Content.ReadFromJsonAsync<BookDetailResponse>()
            ?? throw new InvalidOperationException($"'{slug}' returned no body.");

        Assert.Equal(slug, book.Slug);
        Assert.False(string.IsNullOrWhiteSpace(book.Title));
        Assert.NotEmpty(book.Authors);
    }

    [Fact]
    public async Task A_book_with_no_chapters_reports_none_rather_than_omitting_the_list()
    {
        var book = await _client.GetFromJsonAsync<BookDetailResponse>(
            $"/api/books/{SeededCatalogue.SlugWithoutChapters}")
            ?? throw new InvalidOperationException("The book returned no body.");

        Assert.Empty(book.Chapters);
        Assert.Equal(0, book.ChapterCount);
        Assert.Equal(0, book.TotalRunningTimeSeconds);
    }

    [Fact]
    public async Task A_book_with_no_description_says_so_with_a_null_rather_than_an_empty_string()
    {
        var book = await _client.GetFromJsonAsync<BookDetailResponse>(
            $"/api/books/{SeededCatalogue.SlugWithoutDescription}")
            ?? throw new InvalidOperationException("The book returned no body.");

        Assert.Null(book.Description);
    }

    /// <summary>
    /// The body with the two things that legitimately differ between any two
    /// requests taken out: the slug that was asked for, and the trace identifier
    /// the problem details handler adds for diagnostics. What is left is the part
    /// that would leak the difference between a draft and an absent book.
    /// </summary>
    private async Task<string> ComparableNotFoundBodyAsync(string slug)
    {
        var response = await _client.GetAsync($"/api/books/{slug}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        return TraceId().Replace(body.Replace(slug, "<slug>", StringComparison.Ordinal), "<traceId>");
    }

    [GeneratedRegex("\"traceId\":\"[^\"]*\"")]
    private static partial Regex TraceId();
}
