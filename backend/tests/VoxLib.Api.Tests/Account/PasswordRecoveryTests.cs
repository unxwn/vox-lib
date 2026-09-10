using System.Net;
using VoxLib.Api.Tests.Infrastructure;

namespace VoxLib.Api.Tests.Account;

/// <summary>
/// User Story 4. Getting back into an account nobody can otherwise reach.
/// <para>
/// This story is why lockout and an unrecoverable hash are safe to ship at all.
/// Without it, a forgotten password destroys an account permanently, which for
/// somebody navigating by screen reader is a likely event rather than a rare
/// one.
/// </para>
/// </summary>
[Collection(AccountCollection.Name)]
public class PasswordRecoveryTests(AccountApiFixture fixture)
{
    private const string NewPassword = "новий-довгий-пароль-2026";

    [Fact]
    public async Task The_link_sets_a_new_password_and_the_person_signs_in_with_it()
    {
        var email = await Accounts.ConfirmedAsync(fixture);
        var client = fixture.CreateSeparateBrowser();

        await RequestRecoveryAsync(client, email);

        var (accountId, token) = fixture.Email.LinkTo(email);

        var reset = await AccountApiFixture.PostAsync(
            client,
            "/api/account/recoveries",
            new { accountId, token, password = NewPassword });

        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);

        var signIn = await AccountApiFixture.PostAsync(
            fixture.CreateSeparateBrowser(),
            "/api/account/session",
            new { email, password = NewPassword });

        Assert.Equal(HttpStatusCode.OK, signIn.StatusCode);
    }

    /// <summary>
    /// FR-021. A lockout is what sent many people here in the first place, so
    /// leaving it in force would mean recovering an account and still being
    /// unable to use it.
    /// </summary>
    [Fact]
    public async Task Setting_a_password_clears_a_lockout_that_was_in_force()
    {
        var email = await Accounts.ConfirmedAsync(fixture);
        var guesser = fixture.CreateSeparateBrowser();

        HttpResponseMessage? locked = null;

        for (var attempt = 0; attempt < 15 && locked is null; attempt++)
        {
            var response = await AccountApiFixture.PostAsync(
                guesser,
                "/api/account/session",
                new { email, password = $"невдала спроба {attempt}" });

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                locked = response;
            }
        }

        Assert.NotNull(locked);

        var client = fixture.CreateSeparateBrowser();
        await RequestRecoveryAsync(client, email);

        var (accountId, token) = fixture.Email.LinkTo(email);

        await AccountApiFixture.PostAsync(
            client,
            "/api/account/recoveries",
            new { accountId, token, password = NewPassword });

        var signIn = await AccountApiFixture.PostAsync(
            fixture.CreateSeparateBrowser(),
            "/api/account/session",
            new { email, password = NewPassword });

        Assert.Equal(HttpStatusCode.OK, signIn.StatusCode);
    }

    /// <summary>
    /// FR-022 and SC-006, and the single most important assertion in this
    /// feature. Recovering an account that somebody else is holding has to evict
    /// them, and it has to do so on that session's very next request rather than
    /// at some later interval.
    /// </summary>
    [Fact]
    public async Task Recovering_refuses_a_session_that_existed_beforehand_on_its_next_request()
    {
        var (taker, email) = await Accounts.SignedInAsync(fixture);

        Assert.True((await Accounts.ReadSessionAsync(taker)).SignedIn);

        var owner = fixture.CreateSeparateBrowser();
        await RequestRecoveryAsync(owner, email);

        var (accountId, token) = fixture.Email.LinkTo(email);

        await AccountApiFixture.PostAsync(
            owner,
            "/api/account/recoveries",
            new { accountId, token, password = NewPassword });

        // No wait of any kind. This passes only because the session is
        // revalidated against the store on every request rather than on a timer.
        Assert.False((await Accounts.ReadSessionAsync(taker)).SignedIn);
    }

    /// <summary>
    /// FR-023. Following a link sent to the address proves what confirmation
    /// proves, so an account that was never confirmed must not be left in a
    /// state it cannot leave.
    /// </summary>
    [Fact]
    public async Task Recovering_an_unconfirmed_account_also_confirms_its_address()
    {
        var email = await Accounts.RegisteredAsync(fixture);
        var client = fixture.CreateSeparateBrowser();

        await RequestRecoveryAsync(client, email);

        var (accountId, token) = fixture.Email.LinkTo(email);

        var reset = await AccountApiFixture.PostAsync(
            client,
            "/api/account/recoveries",
            new { accountId, token, password = NewPassword });

        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);

        // Signing in would answer 403 rather than 200 if the address were still
        // unconfirmed, which is the dead end this requirement removes.
        var signIn = await AccountApiFixture.PostAsync(
            fixture.CreateSeparateBrowser(),
            "/api/account/session",
            new { email, password = NewPassword });

        Assert.Equal(HttpStatusCode.OK, signIn.StatusCode);
    }

    /// <summary>
    /// FR-024's first half. Setting a password rotates the security stamp the
    /// token carries, so a used link stops working without any table of issued
    /// links to maintain.
    /// </summary>
    [Fact]
    public async Task A_link_that_has_been_used_no_longer_works()
    {
        var email = await Accounts.ConfirmedAsync(fixture);
        var client = fixture.CreateSeparateBrowser();

        await RequestRecoveryAsync(client, email);

        var (accountId, token) = fixture.Email.LinkTo(email);
        var body = new { accountId, token, password = NewPassword };

        var first = await AccountApiFixture.PostAsync(client, "/api/account/recoveries", body);
        var second = await AccountApiFixture.PostAsync(client, "/api/account/recoveries", body);

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.Gone, second.StatusCode);
    }

    /// <summary>FR-024's second half: a token that was never valid is refused the same way.</summary>
    [Fact]
    public async Task A_token_that_was_never_valid_is_refused()
    {
        var email = await Accounts.ConfirmedAsync(fixture);
        var client = fixture.CreateSeparateBrowser();

        await RequestRecoveryAsync(client, email);

        var (accountId, _) = fixture.Email.LinkTo(email);

        var response = await AccountApiFixture.PostAsync(
            client,
            "/api/account/recoveries",
            new { accountId, token = "вигаданий-токен", password = NewPassword });

        Assert.Equal(HttpStatusCode.Gone, response.StatusCode);
    }

    /// <summary>
    /// A password below the policy is a different failure from an expired link,
    /// and the person needs to be able to tell which happened: one is worth
    /// retyping, the other needs a new message.
    /// </summary>
    [Fact]
    public async Task A_password_below_the_policy_is_refused_without_spending_the_link()
    {
        var email = await Accounts.ConfirmedAsync(fixture);
        var client = fixture.CreateSeparateBrowser();

        await RequestRecoveryAsync(client, email);

        var (accountId, token) = fixture.Email.LinkTo(email);

        var refused = await AccountApiFixture.PostAsync(
            client,
            "/api/account/recoveries",
            new { accountId, token, password = "закоротко" });

        Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);

        // The link still works, so a mistyped password does not cost the person
        // their way back in.
        var accepted = await AccountApiFixture.PostAsync(
            client,
            "/api/account/recoveries",
            new { accountId, token, password = NewPassword });

        Assert.Equal(HttpStatusCode.NoContent, accepted.StatusCode);
    }

    /// <summary>The old password stops working, which is the point of setting a new one.</summary>
    [Fact]
    public async Task The_old_password_stops_working()
    {
        var email = await Accounts.ConfirmedAsync(fixture);
        var client = fixture.CreateSeparateBrowser();

        await RequestRecoveryAsync(client, email);

        var (accountId, token) = fixture.Email.LinkTo(email);

        await AccountApiFixture.PostAsync(
            client,
            "/api/account/recoveries",
            new { accountId, token, password = NewPassword });

        var withOld = await AccountApiFixture.PostAsync(
            fixture.CreateSeparateBrowser(),
            "/api/account/session",
            new { email, password = Accounts.Password });

        Assert.Equal(HttpStatusCode.Unauthorized, withOld.StatusCode);
    }

    /// <summary>Requesting recovery for an address with no account creates and sends nothing.</summary>
    [Fact]
    public async Task Requesting_recovery_for_an_unknown_address_sends_nothing()
    {
        var unknown = Addresses.Fresh();
        var client = fixture.CreateSeparateBrowser();

        var response = await RequestRecoveryAsync(client, unknown);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal(0, fixture.Email.CountTo(unknown));
    }

    private static Task<HttpResponseMessage> RequestRecoveryAsync(
        HttpClient client,
        string email) =>
        AccountApiFixture.PostAsync(client, "/api/account/recovery-requests", new { email });
}
