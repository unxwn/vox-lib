using System.Net;
using VoxLib.Api.Tests.Infrastructure;

namespace VoxLib.Api.Tests.Account;

/// <summary>
/// User Story 5. Ending a session, which is the only thing that makes a borrowed
/// device safe to hand back.
/// </summary>
[Collection(AccountCollection.Name)]
public class SignOutTests(AccountApiFixture fixture)
{
    [Fact]
    public async Task The_request_after_signing_out_is_anonymous()
    {
        var (client, _) = await Accounts.SignedInAsync(fixture);

        var response = await AccountApiFixture.DeleteAsync(client, "/api/account/session");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.False((await Accounts.ReadSessionAsync(client)).SignedIn);
    }

    /// <summary>
    /// FR-018's second half, and the reason signing out is more than clearing a
    /// cookie: a session captured before it happened must not work afterwards.
    /// </summary>
    [Fact]
    public async Task A_session_captured_before_signing_out_is_refused_afterwards()
    {
        var email = await Accounts.ConfirmedAsync(fixture);
        var (client, jar) = fixture.CreateBrowserWithJar();

        var signIn = await AccountApiFixture.PostAsync(
            client,
            "/api/account/session",
            new { email, password = Accounts.Password });

        signIn.EnsureSuccessStatusCode();

        // What somebody who copied the cookie while it worked would be holding.
        var captured = jar
            .GetCookies(new Uri("http://localhost"))
            .Cast<Cookie>()
            .Single(cookie => cookie.Name == "vox_lib_session");

        Assert.True((await Accounts.ReadSessionAsync(client)).SignedIn);

        await AccountApiFixture.DeleteAsync(client, "/api/account/session");

        // Replayed from a browser that never signed in, so nothing but the
        // captured cookie is carrying it.
        var replay = fixture.CreateSeparateBrowser();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/account/session");
        request.Headers.Add("Cookie", $"{captured.Name}={captured.Value}");

        var response = await replay.SendAsync(request);

        Assert.Contains(
            "\"signedIn\":false",
            await response.Content.ReadAsStringAsync(),
            StringComparison.Ordinal);
    }

    /// <summary>
    /// Signing out is per device. A person who signs out at a library computer
    /// must not be signed out on their phone.
    /// </summary>
    [Fact]
    public async Task Signing_out_on_one_device_leaves_another_signed_in()
    {
        var email = await Accounts.ConfirmedAsync(fixture);

        var phone = fixture.CreateSeparateBrowser();
        var shared = fixture.CreateSeparateBrowser();

        foreach (var device in new[] { phone, shared })
        {
            var response = await AccountApiFixture.PostAsync(
                device,
                "/api/account/session",
                new { email, password = Accounts.Password });

            response.EnsureSuccessStatusCode();
        }

        await AccountApiFixture.DeleteAsync(shared, "/api/account/session");

        Assert.False((await Accounts.ReadSessionAsync(shared)).SignedIn);
        Assert.True((await Accounts.ReadSessionAsync(phone)).SignedIn);
    }

    /// <summary>
    /// Signing out with no session is not an error. A person on a shared device
    /// who presses it twice, or whose session already expired, needs the same
    /// reassuring answer rather than a failure to interpret.
    /// </summary>
    [Fact]
    public async Task Signing_out_twice_is_not_an_error()
    {
        var (client, _) = await Accounts.SignedInAsync(fixture);

        var first = await AccountApiFixture.DeleteAsync(client, "/api/account/session");
        var second = await AccountApiFixture.DeleteAsync(client, "/api/account/session");

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
    }

    /// <summary>
    /// Signing out changes state, so it needs the same proof of origin every
    /// other state-changing request needs. Without it, any site could sign a
    /// visitor out of this one.
    /// </summary>
    [Fact]
    public async Task Signing_out_without_the_antiforgery_header_is_refused()
    {
        var (client, _) = await Accounts.SignedInAsync(fixture);

        var response = await client.DeleteAsync("/api/account/session");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await Accounts.ReadSessionAsync(client)).SignedIn);
    }
}
