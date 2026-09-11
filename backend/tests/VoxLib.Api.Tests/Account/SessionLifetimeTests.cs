using System.Net;
using VoxLib.Api.Tests.Infrastructure;

namespace VoxLib.Api.Tests.Account;

/// <summary>
/// FR-012, FR-013 and SC-004: the session survives closing the browser, and the
/// page cannot read it.
/// </summary>
[Collection(AccountCollection.Name)]
public class SessionLifetimeTests(AccountApiFixture fixture)
{
    /// <summary>
    /// What "closed the browser and reopened it" means to a test: the client is
    /// thrown away and another is built over the same cookie jar.
    /// </summary>
    [Fact]
    public async Task A_person_who_closes_the_browser_is_still_recognised_when_they_return()
    {
        fixture.NewBrowser();

        var email = await Accounts.ConfirmedAsync(fixture);

        using (var first = fixture.CreateBrowser())
        {
            var response = await AccountApiFixture.PostAsync(
                first,
                "/api/account/session",
                new { email, password = Accounts.Password });

            response.EnsureSuccessStatusCode();
        }

        // A different client, the same browser.
        using var returning = fixture.CreateBrowser();

        var session = await Accounts.ReadSessionAsync(returning);

        Assert.True(session.SignedIn);
        Assert.Equal(email, session.Email);
    }

    /// <summary>
    /// FR-013: a script injected into a page must not be able to read the
    /// session and carry it elsewhere. The cookie is also Strict, so another
    /// site cannot cause it to be sent.
    /// </summary>
    [Fact]
    public async Task The_session_cookie_cannot_be_read_by_a_script_and_is_not_sent_from_elsewhere()
    {
        var email = await Accounts.ConfirmedAsync(fixture);
        var client = fixture.CreateSeparateBrowser();

        var response = await AccountApiFixture.PostAsync(
            client,
            "/api/account/session",
            new { email, password = Accounts.Password });

        var cookie = SessionCookie(response);

        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// SC-004 asks for thirty days without signing in again, which means the
    /// cookie has to outlive the browser session rather than being cleared on
    /// exit.
    /// </summary>
    [Fact]
    public async Task The_session_outlives_the_browser_session_by_about_a_month()
    {
        var email = await Accounts.ConfirmedAsync(fixture);
        var client = fixture.CreateSeparateBrowser();

        var response = await AccountApiFixture.PostAsync(
            client,
            "/api/account/session",
            new { email, password = Accounts.Password });

        var cookie = SessionCookie(response);

        Assert.Contains("expires=", cookie, StringComparison.OrdinalIgnoreCase);

        var expiry = DateTimeOffset.Parse(
            cookie
                .Split(';')
                .Select(part => part.Trim())
                .Single(part => part.StartsWith("expires=", StringComparison.OrdinalIgnoreCase))
                ["expires=".Length..]);

        var days = (expiry - DateTimeOffset.UtcNow).TotalDays;

        Assert.InRange(days, 29, 31);
    }

    /// <summary>An anonymous browser gets an answer, not a challenge.</summary>
    [Fact]
    public async Task An_anonymous_browser_is_told_it_is_anonymous_rather_than_challenged()
    {
        var client = fixture.CreateSeparateBrowser();

        var response = await client.GetAsync("/api/account/session");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(response.Headers.WwwAuthenticate);
        Assert.False((await Accounts.ReadSessionAsync(client)).SignedIn);
    }

    private static string SessionCookie(HttpResponseMessage response) =>
        response.Headers
            .Where(header => header.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase))
            .SelectMany(header => header.Value)
            .Single(cookie => cookie.StartsWith("vox_lib_session=", StringComparison.Ordinal));
}
