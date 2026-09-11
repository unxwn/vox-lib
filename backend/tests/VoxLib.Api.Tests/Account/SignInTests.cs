using System.Net;
using VoxLib.Api.Tests.Infrastructure;

namespace VoxLib.Api.Tests.Account;

/// <summary>
/// User Story 3. Signing in, and the three ways it can be refused.
/// <para>
/// The refusals are the security-critical part. An unknown address and a wrong
/// password answer alike; "the address is not confirmed" is reachable only once
/// the password has verified, because that is the single case FR-005 allows the
/// product to admit an account exists.
/// </para>
/// </summary>
[Collection(AccountCollection.Name)]
public class SignInTests(AccountApiFixture fixture)
{
    private const string Password = "довгий-пароль-2026";

    [Fact]
    public async Task A_confirmed_account_signs_in_and_the_session_says_who_it_is()
    {
        var email = await Accounts.ConfirmedAsync(fixture);
        var client = fixture.CreateSeparateBrowser();

        var response = await AccountApiFixture.PostAsync(
            client,
            "/api/account/session",
            new { email, password = Password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var session = await Accounts.ReadSessionAsync(client);

        Assert.True(session.SignedIn);
        Assert.Equal(email, session.Email);
    }

    [Fact]
    public async Task A_wrong_password_is_refused()
    {
        var email = await Accounts.ConfirmedAsync(fixture);
        var client = fixture.CreateSeparateBrowser();

        var response = await AccountApiFixture.PostAsync(
            client,
            "/api/account/session",
            new { email, password = "зовсім інший пароль" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False((await Accounts.ReadSessionAsync(client)).SignedIn);
    }

    /// <summary>
    /// FR-011. The reason is given, and it is given only after the password
    /// supplied was correct, so it cannot be used to discover whether an address
    /// has an account.
    /// </summary>
    [Fact]
    public async Task The_correct_password_on_an_unconfirmed_address_says_so()
    {
        var email = await Accounts.RegisteredAsync(fixture);
        var client = fixture.CreateSeparateBrowser();

        var response = await AccountApiFixture.PostAsync(
            client,
            "/api/account/session",
            new { email, password = Password });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("підтвердьте", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The same unconfirmed account with the wrong password must answer 401, not
    /// 403. A 403 here would say "this address has an account" to anyone who
    /// asked, which is the disclosure FR-005 exists to prevent.
    /// </summary>
    [Fact]
    public async Task An_unconfirmed_address_with_a_wrong_password_gives_nothing_away()
    {
        var email = await Accounts.RegisteredAsync(fixture);
        var client = fixture.CreateSeparateBrowser();

        var response = await AccountApiFixture.PostAsync(
            client,
            "/api/account/session",
            new { email, password = "не той пароль зовсім" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>FR-008 and FR-010 together: confirming makes the account usable.</summary>
    [Fact]
    public async Task Confirming_turns_a_refused_sign_in_into_a_successful_one()
    {
        var email = await Accounts.RegisteredAsync(fixture);
        var client = fixture.CreateSeparateBrowser();

        var before = await AccountApiFixture.PostAsync(
            client,
            "/api/account/session",
            new { email, password = Password });

        Assert.Equal(HttpStatusCode.Forbidden, before.StatusCode);

        var (accountId, token) = fixture.Email.LinkTo(email);
        await AccountApiFixture.PostAsync(
            client,
            "/api/account/confirmations",
            new { accountId, token });

        var after = await AccountApiFixture.PostAsync(
            client,
            "/api/account/session",
            new { email, password = Password });

        Assert.Equal(HttpStatusCode.OK, after.StatusCode);
    }

    /// <summary>
    /// FR-025 and FR-026: the status an access decision will be made from is
    /// carried, and defaults to false because there is nobody to verify against
    /// yet.
    /// </summary>
    [Fact]
    public async Task A_new_account_is_not_a_verified_beneficiary()
    {
        var email = await Accounts.ConfirmedAsync(fixture);
        var client = fixture.CreateSeparateBrowser();

        await AccountApiFixture.PostAsync(
            client,
            "/api/account/session",
            new { email, password = Password });

        Assert.False((await Accounts.ReadSessionAsync(client)).IsVerifiedBeneficiary);
    }

    /// <summary>The address is matched the way people type it, not the way they stored it.</summary>
    [Fact]
    public async Task The_address_is_matched_without_regard_to_letter_case()
    {
        var email = await Accounts.ConfirmedAsync(fixture);
        var client = fixture.CreateSeparateBrowser();

        var response = await AccountApiFixture.PostAsync(
            client,
            "/api/account/session",
            new { email = email.ToUpperInvariant(), password = Password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
