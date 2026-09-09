using System.Net;
using System.Net.Http.Headers;
using VoxLib.Api.Tests.Infrastructure;

namespace VoxLib.Api.Tests.Catalogue;

/// <summary>
/// FR-016: the catalogue is readable with no account. The people this exists for
/// have to be able to find a book before they have any reason to register, so
/// this is a requirement rather than a convenience.
/// </summary>
[Collection(CatalogueCollection.Name)]
public class AnonymousAccessTests(CatalogueApiFixture fixture)
{
    private readonly HttpClient _client = fixture.CreateClient();

    [Theory]
    [InlineData("/api/books")]
    [InlineData("/api/books?page=2")]
    public async Task The_catalogue_is_readable_with_no_credentials_of_any_kind(string path)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);

        Assert.Null(request.Headers.Authorization);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task No_response_asks_the_visitor_to_authenticate()
    {
        var response = await _client.GetAsync("/api/books");

        Assert.Empty(response.Headers.WwwAuthenticate);
        Assert.DoesNotContain(
            response.Headers,
            header => header.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// A credential offered where none is wanted must not change the answer.
    /// Refusing an unexpected token would be a gate arriving by accident.
    /// </summary>
    [Fact]
    public async Task An_unnecessary_credential_is_ignored_rather_than_refused()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/books");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-real-token");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
