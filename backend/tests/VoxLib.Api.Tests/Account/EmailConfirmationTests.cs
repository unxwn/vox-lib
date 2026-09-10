using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VoxLib.Api.Tests.Infrastructure;

namespace VoxLib.Api.Tests.Account;

/// <summary>
/// User Story 2. Following the link that was sent, twice, and after it expired.
/// <para>
/// FR-008 draws a distinction most implementations lose: an address that is
/// already confirmed is not an error, and an expired link is. Following the same
/// link twice is ordinary, and reporting it as a failure sends a person looking
/// for a problem that is not there.
/// </para>
/// </summary>
[Collection(AccountCollection.Name)]
public class EmailConfirmationTests(AccountApiFixture fixture)
{
    private const string Password = "довгий-пароль-2026";

    [Fact]
    public async Task Following_the_link_confirms_the_address()
    {
        var email = Addresses.Fresh();
        var client = fixture.CreateSeparateBrowser();

        await RegisterAsync(client, email);

        var (accountId, token) = fixture.Email.LinkTo(email);

        var response = await AccountApiFixture.PostAsync(
            client,
            "/api/account/confirmations",
            new { accountId, token });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("confirmed", body.GetProperty("outcome").GetString());

        var confirmed = await fixture.ReadAsync(database =>
            database.Users
                .Where(account => account.Email == email)
                .Select(account => account.EmailConfirmed)
                .SingleAsync());

        Assert.True(confirmed);
    }

    /// <summary>
    /// FR-008's first half. A person forwards the message to themselves, or the
    /// mail client prefetches the link, or they simply click it again.
    /// </summary>
    [Fact]
    public async Task Following_the_same_link_again_says_the_address_is_already_confirmed()
    {
        var email = Addresses.Fresh();
        var client = fixture.CreateSeparateBrowser();

        await RegisterAsync(client, email);

        var (accountId, token) = fixture.Email.LinkTo(email);
        var body = new { accountId, token };

        await AccountApiFixture.PostAsync(client, "/api/account/confirmations", body);
        var again = await AccountApiFixture.PostAsync(client, "/api/account/confirmations", body);

        Assert.Equal(HttpStatusCode.OK, again.StatusCode);

        var result = await again.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("alreadyConfirmed", result.GetProperty("outcome").GetString());
    }

    /// <summary>
    /// The order of checks is what makes this work: whether the address is
    /// already confirmed is asked before the token is validated. Confirming does
    /// not rotate the security stamp, so the token stays technically valid, and
    /// the distinction has to come from somewhere else.
    /// </summary>
    [Fact]
    public async Task An_already_confirmed_address_says_so_even_when_the_token_is_worthless()
    {
        var email = Addresses.Fresh();
        var client = fixture.CreateSeparateBrowser();

        await RegisterAsync(client, email);

        var (accountId, token) = fixture.Email.LinkTo(email);

        await AccountApiFixture.PostAsync(
            client,
            "/api/account/confirmations",
            new { accountId, token });

        var withNonsense = await AccountApiFixture.PostAsync(
            client,
            "/api/account/confirmations",
            new { accountId, token = "це-точно-не-справжній-токен" });

        Assert.Equal(HttpStatusCode.OK, withNonsense.StatusCode);

        var result = await withNonsense.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("alreadyConfirmed", result.GetProperty("outcome").GetString());
    }

    /// <summary>FR-008's second half: an expired or wrong link is refused and told apart.</summary>
    [Fact]
    public async Task A_token_that_was_never_valid_is_refused_and_a_replacement_offered()
    {
        var email = Addresses.Fresh();
        var client = fixture.CreateSeparateBrowser();

        await RegisterAsync(client, email);

        var (accountId, _) = fixture.Email.LinkTo(email);

        var response = await AccountApiFixture.PostAsync(
            client,
            "/api/account/confirmations",
            new { accountId, token = "давно-протермінований-токен" });

        Assert.Equal(HttpStatusCode.Gone, response.StatusCode);

        var problem = await response.Content.ReadAsStringAsync();
        Assert.Contains("посилання", problem, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>A token issued for one account must not confirm another.</summary>
    [Fact]
    public async Task A_token_for_one_account_does_not_confirm_a_different_one()
    {
        var client = fixture.CreateSeparateBrowser();

        var mine = Addresses.Fresh();
        var theirs = Addresses.Fresh();

        await RegisterAsync(client, mine);
        var (_, myToken) = fixture.Email.LinkTo(mine);

        await RegisterAsync(client, theirs);
        var (theirId, _) = fixture.Email.LinkTo(theirs);

        var response = await AccountApiFixture.PostAsync(
            client,
            "/api/account/confirmations",
            new { accountId = theirId, token = myToken });

        Assert.Equal(HttpStatusCode.Gone, response.StatusCode);
    }

    /// <summary>
    /// FR-009: another message can be asked for, because delivery is outside
    /// this system's control and confirmation is on the critical path.
    /// </summary>
    [Fact]
    public async Task Another_confirmation_message_can_be_requested()
    {
        var email = Addresses.Fresh();
        var client = fixture.CreateSeparateBrowser();

        await RegisterAsync(client, email);
        var before = fixture.Email.CountTo(email);

        var response = await AccountApiFixture.PostAsync(
            client,
            "/api/account/confirmation-requests",
            new { email });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal(before + 1, fixture.Email.CountTo(email));

        // And the new link works, so a replacement is a real replacement.
        var (accountId, token) = fixture.Email.LinkTo(email);

        var confirmation = await AccountApiFixture.PostAsync(
            client,
            "/api/account/confirmations",
            new { accountId, token });

        Assert.Equal(HttpStatusCode.OK, confirmation.StatusCode);
    }

    /// <summary>
    /// FR-009's other half. Without a limit this endpoint is a way to send mail
    /// repeatedly to someone who did not ask for it.
    /// </summary>
    [Fact]
    public async Task Repeated_requests_for_one_address_stop_producing_messages()
    {
        var email = Addresses.Fresh();
        var client = fixture.CreateSeparateBrowser();

        await RegisterAsync(client, email);

        for (var attempt = 0; attempt < 10; attempt++)
        {
            var response = await AccountApiFixture.PostAsync(
                client,
                "/api/account/confirmation-requests",
                new { email });

            // Always accepted, whatever happened: saying "you are being limited
            // on this address" would itself be a signal about the address.
            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        }

        Assert.True(
            fixture.Email.CountTo(email) < 8,
            "The limit has to stop messages being sent, not merely change the answer.");
    }

    /// <summary>
    /// Asking for another message for an address with no account must look the
    /// same and must create nothing.
    /// </summary>
    [Fact]
    public async Task Requesting_confirmation_for_an_unknown_address_is_accepted_and_sends_nothing()
    {
        var unknown = Addresses.Fresh();
        var client = fixture.CreateSeparateBrowser();

        var response = await AccountApiFixture.PostAsync(
            client,
            "/api/account/confirmation-requests",
            new { email = unknown });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal(0, fixture.Email.CountTo(unknown));
    }

    private static Task<HttpResponseMessage> RegisterAsync(HttpClient client, string email) =>
        AccountApiFixture.PostAsync(
            client,
            "/api/account/registrations",
            new { email, password = Password });
}
