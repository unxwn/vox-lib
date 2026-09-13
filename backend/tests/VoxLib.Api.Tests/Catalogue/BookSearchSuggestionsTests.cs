using System.Net.Http.Json;
using VoxLib.Api.Tests.Infrastructure;

namespace VoxLib.Api.Tests.Catalogue;

/// <summary>
/// SC-010: every suggestion the endpoint offers actually finds something.
/// <para>
/// This is the test the whole idea rests on. A suggestion that returns nothing
/// is worse than no suggestion at all — it teaches a visitor that the search is
/// broken — and the guarantee holds only because the term is a published row's
/// own text, copied verbatim. So the assertion is made by running the search
/// rather than by inspecting the string.
/// </para>
/// </summary>
[Collection(SampleCatalogueCollection.Name)]
public class BookSearchSuggestionsTests(SampleCatalogueApiFixture fixture)
{
    private readonly HttpClient _client = fixture.CreateClient();

    [Fact]
    public async Task Every_suggestion_searched_for_returns_at_least_one_result()
    {
        var suggestions = await SampleAsync();

        Assert.NotEmpty(suggestions.Items);

        foreach (var suggestion in suggestions.Items)
        {
            var results = await _client.GetFromJsonAsync<PagedBooksResponse>(
                $"/api/books?q={Uri.EscapeDataString(suggestion.Term)}")
                ?? throw new InvalidOperationException("The search returned no body.");

            Assert.True(
                results.TotalCount > 0,
                $"The suggestion '{suggestion.Term}' ({suggestion.Kind}) finds nothing.");
        }
    }

    /// <summary>
    /// The shape rather than the content, as the clarification settled: the
    /// sample is random per request, so asserting which terms come back would
    /// be asserting the outcome of a shuffle.
    /// </summary>
    [Fact]
    public async Task A_suggestion_says_what_it_was_drawn_from()
    {
        var suggestions = await SampleAsync();

        Assert.All(suggestions.Items, suggestion =>
        {
            Assert.False(string.IsNullOrWhiteSpace(suggestion.Term));
            Assert.Contains(suggestion.Kind, new[] { "title", "author" });
        });
    }

    [Fact]
    public async Task At_most_five_are_offered()
    {
        var suggestions = await SampleAsync();

        Assert.InRange(suggestions.Items.Count, 1, 5);
    }

    /// <summary>
    /// Load-bearing rather than tidy: a cached response freezes one visitor's
    /// handful forever while staying fresh on the server, so the field would
    /// show the same example on every visit for the rest of the session.
    /// </summary>
    [Fact]
    public async Task The_response_may_not_be_cached()
    {
        var response = await _client.GetAsync("/api/search-suggestions");

        Assert.True(response.Headers.CacheControl?.NoStore);
    }

    /// <summary>
    /// Both halves of FR-022 read one response, so the titles and the names have
    /// to arrive together rather than as two samples that can disagree.
    /// </summary>
    [Fact]
    public async Task Titles_and_author_names_are_both_offered()
    {
        // Sampled at random, so one response can legitimately be short on a
        // small catalogue. Several reads establish that both kinds occur.
        var kinds = new HashSet<string>(StringComparer.Ordinal);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            foreach (var suggestion in (await SampleAsync()).Items)
            {
                kinds.Add(suggestion.Kind);
            }
        }

        Assert.Contains("title", kinds);
        Assert.Contains("author", kinds);
    }

    private async Task<SearchSuggestionsResponse> SampleAsync() =>
        await _client.GetFromJsonAsync<SearchSuggestionsResponse>("/api/search-suggestions")
        ?? throw new InvalidOperationException("The endpoint returned no body.");
}
