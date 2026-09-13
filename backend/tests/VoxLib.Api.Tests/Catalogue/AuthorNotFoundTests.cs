using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using VoxLib.Api.Tests.Infrastructure;

namespace VoxLib.Api.Tests.Catalogue;

/// <summary>
/// FR-037: an author with nothing a visitor may see is not found, exactly as an
/// absent one is. A page listing no books would be a dead end wearing a name.
/// </summary>
[Collection(EmptyCatalogueCollection.Name)]
public partial class AuthorNotFoundTests(EmptyCatalogueApiFixture fixture)
{
    private readonly HttpClient _client = fixture.CreateClient();

    [Fact]
    public async Task An_absent_author_is_not_found()
    {
        var response = await _client.GetAsync("/api/authors/no-such-person-at-all");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    /// <summary>
    /// The empty catalogue fixture deletes the books but leaves nobody behind
    /// with a published one, so every author in it is in exactly the state
    /// FR-037 describes. Asserting the two answers are identical is what proves
    /// the catalogue does not disclose that somebody exists but is unpublished.
    /// </summary>
    [Fact]
    public async Task An_author_with_no_published_books_answers_exactly_as_an_absent_one()
    {
        var unpublished = await ComparableBodyAsync("dzhoko-villink");
        var absent = await ComparableBodyAsync("no-such-person-at-all");

        Assert.Equal(absent, unpublished);
    }

    [Fact]
    public async Task The_index_is_empty_rather_than_listing_names_that_lead_nowhere()
    {
        var index = await _client.GetFromJsonAsync<AuthorIndexResponse>("/api/authors")
            ?? throw new InvalidOperationException("The index returned no body.");

        Assert.Empty(index.Items);
    }

    private async Task<string> ComparableBodyAsync(string slug)
    {
        var response = await _client.GetAsync($"/api/authors/{slug}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        return TraceId().Replace(body.Replace(slug, "<slug>", StringComparison.Ordinal), "<traceId>");
    }

    [GeneratedRegex("\"traceId\":\"[^\"]*\"")]
    private static partial Regex TraceId();
}
