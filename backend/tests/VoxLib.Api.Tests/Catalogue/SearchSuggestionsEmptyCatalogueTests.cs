using System.Net;
using System.Net.Http.Json;
using VoxLib.Api.Tests.Infrastructure;

namespace VoxLib.Api.Tests.Catalogue;

/// <summary>
/// An empty catalogue offers no suggestions, and that is a state rather than an
/// error: the field falls back to its neutral placeholder, shows no links, and
/// stays fully usable. Answering 404 or 500 here would take the search field
/// down with the thing that only decorates it.
/// </summary>
[Collection(EmptyCatalogueCollection.Name)]
public class SearchSuggestionsEmptyCatalogueTests(EmptyCatalogueApiFixture fixture)
{
    private readonly HttpClient _client = fixture.CreateClient();

    [Fact]
    public async Task An_empty_catalogue_offers_an_empty_list_rather_than_an_error()
    {
        var response = await _client.GetAsync("/api/search-suggestions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var suggestions = await response.Content.ReadFromJsonAsync<SearchSuggestionsResponse>()
            ?? throw new InvalidOperationException("The endpoint returned no body.");

        Assert.Empty(suggestions.Items);
    }
}
