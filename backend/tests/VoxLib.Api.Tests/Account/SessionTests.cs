using System.Net;
using VoxLib.Api.Tests.Infrastructure;

namespace VoxLib.Api.Tests.Account;

/// <summary>
/// What the session endpoint says about who is signed in.
/// <para>
/// It answers 200 for an anonymous browser rather than 401, deliberately. This
/// is how every page decides whether to show a sign-in link or a sign-out one,
/// and how the interface detects a browser that is discarding the session, so a
/// challenge for the ordinary case would be noise.
/// </para>
/// </summary>
[Collection(AccountCollection.Name)]
public class SessionTests(AccountApiFixture fixture)
{
    [Fact]
    public async Task A_signed_in_session_carries_the_address_and_the_beneficiary_status()
    {
        var (client, email) = await Accounts.SignedInAsync(fixture);

        var session = await Accounts.ReadSessionAsync(client);

        Assert.True(session.SignedIn);
        Assert.Equal(email, session.Email);

        // FR-025: the field the audio feature will gate on exists and defaults
        // to false, because there is no organisation to verify against yet.
        Assert.False(session.IsVerifiedBeneficiary);
    }

    [Fact]
    public async Task An_anonymous_session_carries_no_address()
    {
        var client = fixture.CreateSeparateBrowser();

        var session = await Accounts.ReadSessionAsync(client);

        Assert.False(session.SignedIn);
        Assert.Null(session.Email);
    }

    /// <summary>
    /// FR-014: a session can be ended from the server and stops working on its
    /// next use, not at some later interval. This is what SC-006 measures, and
    /// it only holds because the stamp is revalidated on every request.
    /// </summary>
    [Fact]
    public async Task A_session_ended_from_the_server_stops_working_on_its_very_next_use()
    {
        var (client, email) = await Accounts.SignedInAsync(fixture);

        Assert.True((await Accounts.ReadSessionAsync(client)).SignedIn);

        await fixture.EndAllSessionsAsync(email);

        // The very next request, with no wait of any kind.
        Assert.False((await Accounts.ReadSessionAsync(client)).SignedIn);
    }

    /// <summary>
    /// Signing in on one device must not disturb another. This is what makes the
    /// eviction in FR-022 meaningful: it is a deliberate act, not a side effect
    /// of ordinary use.
    /// </summary>
    [Fact]
    public async Task Two_devices_can_be_signed_in_to_one_account_at_once()
    {
        var email = await Accounts.ConfirmedAsync(fixture);

        var phone = fixture.CreateSeparateBrowser();
        var laptop = fixture.CreateSeparateBrowser();

        foreach (var device in new[] { phone, laptop })
        {
            var response = await AccountApiFixture.PostAsync(
                device,
                "/api/account/session",
                new { email, password = Accounts.Password });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        Assert.True((await Accounts.ReadSessionAsync(phone)).SignedIn);
        Assert.True((await Accounts.ReadSessionAsync(laptop)).SignedIn);
    }
}
