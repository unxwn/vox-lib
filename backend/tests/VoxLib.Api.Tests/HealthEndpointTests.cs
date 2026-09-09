using System.Net;
using System.Net.Http.Json;
using VoxLib.Api.Tests.Infrastructure;

namespace VoxLib.Api.Tests;

[Collection(CatalogueCollection.Name)]
public class HealthEndpointTests(CatalogueApiFixture fixture)
{
    private readonly HttpClient _client = fixture.CreateClient();

    [Fact]
    public async Task Health_returns_ok()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(body);
        Assert.Equal("ok", body.Status);
    }

    private sealed record HealthResponse(string Status, DateTimeOffset Utc);
}
