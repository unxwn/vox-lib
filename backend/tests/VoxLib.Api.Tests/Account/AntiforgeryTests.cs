using System.Net;
using System.Net.Http.Json;
using VoxLib.Api.Tests.Infrastructure;

namespace VoxLib.Api.Tests.Account;

/// <summary>
/// The session cookie is attached by the browser automatically, so every request
/// that changes state has to prove it came from this site.
/// <para>
/// On .NET 10 the antiforgery middleware validates only endpoints that read form
/// data, and every endpoint here binds JSON, so this is checked in the handlers
/// instead. These tests are what say the check is actually attached.
/// </para>
/// </summary>
[Collection(AccountCollection.Name)]
public class AntiforgeryTests(AccountApiFixture fixture)
{
    [Theory]
    [InlineData("/api/account/registrations")]
    [InlineData("/api/account/session")]
    [InlineData("/api/account/confirmation-requests")]
    [InlineData("/api/account/recovery-requests")]
    public async Task A_state_changing_request_with_no_token_is_refused(string path)
    {
        var client = fixture.CreateSeparateBrowser();

        var response = await client.PostAsJsonAsync(
            path,
            new { email = Addresses.Fresh(), password = "довгий-пароль-2026" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// A token belonging to a different browser must not work, or the guard
    /// would be a formality any site could satisfy by copying a value.
    /// </summary>
    [Fact]
    public async Task A_token_from_another_browser_is_refused()
    {
        var mine = fixture.CreateSeparateBrowser();
        var theirs = fixture.CreateSeparateBrowser();

        var stolen = await AccountApiFixture.TokenAsync(theirs);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/account/registrations")
        {
            Content = JsonContent.Create(
                new { email = Addresses.Fresh(), password = "довгий-пароль-2026" }),
        };

        // My own cookies, their token.
        await AccountApiFixture.TokenAsync(mine);
        request.Headers.Add("X-XSRF-TOKEN", stolen);

        var response = await mine.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// The token endpoint is anonymous, unlike the sample in the framework
    /// documentation. Registering and signing in change state and are made by
    /// someone with no session, and signing in is itself a target of this
    /// attack, so requiring a session to get a token would leave both unguarded.
    /// </summary>
    [Fact]
    public async Task The_token_can_be_obtained_without_a_session()
    {
        var client = fixture.CreateSeparateBrowser();

        var response = await client.GetAsync("/api/account/antiforgery-token");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var cookies = response.Headers
            .Where(header => header.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase))
            .SelectMany(header => header.Value)
            .ToList();

        Assert.Contains(cookies, cookie => cookie.StartsWith("XSRF-TOKEN=", StringComparison.Ordinal));
    }

    /// <summary>
    /// The readable cookie is the one the page echoes. The paired cookie must
    /// stay unreadable, or a script could forge the header from it.
    /// </summary>
    [Fact]
    public async Task The_paired_cookie_cannot_be_read_by_a_script()
    {
        var client = fixture.CreateSeparateBrowser();

        var response = await client.GetAsync("/api/account/antiforgery-token");

        var paired = response.Headers
            .Where(header => header.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase))
            .SelectMany(header => header.Value)
            .Single(cookie => cookie.StartsWith("vox_lib_antiforgery=", StringComparison.Ordinal));

        Assert.Contains("httponly", paired, StringComparison.OrdinalIgnoreCase);
    }
}
