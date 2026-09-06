using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace VoxLib.Api.Tests;

public class WeatherForecastEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Returns_five_forecasts()
    {
        var response = await _client.GetAsync("/api/weatherforecast");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var forecasts = await response.Content.ReadFromJsonAsync<Forecast[]>();
        Assert.NotNull(forecasts);
        Assert.Equal(5, forecasts.Length);
    }

    [Fact]
    public async Task Fahrenheit_is_derived_from_celsius()
    {
        var forecasts = await _client.GetFromJsonAsync<Forecast[]>("/api/weatherforecast");

        Assert.NotNull(forecasts);
        Assert.All(forecasts, f =>
            Assert.Equal(32 + (int)(f.TemperatureC / 0.5556), f.TemperatureF));
    }

    private sealed record Forecast(DateOnly Date, int TemperatureC, int TemperatureF, string? Summary);
}
