using System.Net;
using System.Net.Http.Json;
using VoxLib.Api.Tests.Infrastructure;

namespace VoxLib.Api.Tests.Catalogue;

/// <summary>
/// FR-051: storage being unreachable must not take the catalogue with it.
/// <para>
/// It holds because composing a cover URL is pure string work — it reads
/// configuration and touches no network — so a catalogue response cannot fail
/// on account of storage however dead storage is. Seeding is the only thing
/// that talks to it at startup, and that logs and continues.
/// </para>
/// </summary>
public class CoverStorageOutageTests : IClassFixture<DeadCoverStorageFixture>
{
    private readonly HttpClient _client;

    public CoverStorageOutageTests(DeadCoverStorageFixture fixture) =>
        _client = fixture.CreateClient();

    /// <summary>
    /// The application started at all, which is half the requirement: an
    /// unreachable image store must not be a startup dependency of reading a
    /// book's title.
    /// </summary>
    [Fact]
    public async Task The_application_starts_with_storage_unreachable()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task The_catalogue_still_answers()
    {
        var response = await _client.GetAsync("/api/books");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await response.Content.ReadFromJsonAsync<PagedBooksResponse>()
            ?? throw new InvalidOperationException("The catalogue returned no body.");

        Assert.NotEmpty(page.Items);
    }

    /// <summary>
    /// And still carries cover addresses, which is the part worth stating: they
    /// are composed rather than looked up, so they exist whether or not anything
    /// answers on them. What the browser does when the image fails to load is
    /// CoverArt's problem, and it swaps in the placeholder.
    /// </summary>
    [Fact]
    public async Task Books_still_carry_composed_cover_addresses()
    {
        var page = await _client.GetFromJsonAsync<PagedBooksResponse>("/api/books")
            ?? throw new InvalidOperationException("The catalogue returned no body.");

        var withCover = page.Items.First(book => book.Cover is not null);

        Assert.NotEmpty(withCover.Cover!.Sources);
    }

    [Fact]
    public async Task A_book_page_still_answers()
    {
        var response = await _client.GetAsync(
            $"/api/books/{SeededCatalogue.AuthoredSlug}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task The_author_index_still_answers()
    {
        var response = await _client.GetAsync("/api/authors");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
