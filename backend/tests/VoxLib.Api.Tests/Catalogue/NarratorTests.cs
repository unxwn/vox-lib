using System.Net.Http.Json;
using VoxLib.Api.Tests.Infrastructure;

namespace VoxLib.Api.Tests.Catalogue;

/// <summary>
/// FR-040: the озвучувач is a name on the book, and a book without one says so
/// with a null rather than an empty string.
/// <para>
/// It is the field that tells an audiobook from a book, and what a listener
/// chooses between two recordings by. No book in the shipped catalogue has one,
/// so both halves are proved against a fixture.
/// </para>
/// </summary>
public class NarratorTests : IClassFixture<NarratedBookFixture>
{
    private readonly HttpClient _client;

    public NarratorTests(NarratedBookFixture fixture) => _client = fixture.CreateClient();

    [Fact]
    public async Task A_book_with_a_narrator_returns_their_name()
    {
        var book = await GetAsync(NarratedBookFixture.NarratedSlug);

        Assert.Equal(NarratedBookFixture.NarratorName, book.Narrator);
    }

    [Fact]
    public async Task A_book_without_a_narrator_returns_null_rather_than_an_empty_string()
    {
        var book = await GetAsync(SeededCatalogue.AuthoredSlug);

        Assert.Null(book.Narrator);
    }

    /// <summary>
    /// A name, and nothing that could be mistaken for a file. FR-010 holds on
    /// this field exactly as it does on a chapter.
    /// </summary>
    [Fact]
    public async Task The_narrator_is_a_name_rather_than_anything_to_do_with_a_file()
    {
        var response = await _client.GetAsync($"/api/books/{NarratedBookFixture.NarratedSlug}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("audioUrl", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".mp3", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("storageKey", body, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<BookDetailResponse> GetAsync(string slug) =>
        await _client.GetFromJsonAsync<BookDetailResponse>($"/api/books/{slug}")
        ?? throw new InvalidOperationException($"'{slug}' returned no body.");
}
