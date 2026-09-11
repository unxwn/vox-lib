using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VoxLib.Api.Tests.Infrastructure;

namespace VoxLib.Api.Tests.Account;

/// <summary>
/// User Story 1. Creating an account, and the two things about it that are
/// easy to get wrong: the password policy, and what happens when the same
/// registration arrives twice.
/// </summary>
[Collection(AccountCollection.Name)]
public class RegistrationTests(AccountApiFixture fixture)
{
    private const string GoodPassword = "довгий-пароль-2026";

    [Fact]
    public async Task A_well_formed_submission_creates_exactly_one_account()
    {
        var email = Addresses.Fresh();
        var client = fixture.CreateSeparateBrowser();

        var response = await AccountApiFixture.PostAsync(
            client,
            "/api/account/registrations",
            new { email, password = GoodPassword });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var count = await fixture.ReadAsync(database =>
            database.Users.CountAsync(account => account.Email == email));

        Assert.Equal(1, count);
    }

    /// <summary>FR-004: nothing is usable until a message has been sent.</summary>
    [Fact]
    public async Task Registering_sends_a_message_carrying_a_link()
    {
        var email = Addresses.Fresh();
        var client = fixture.CreateSeparateBrowser();

        await AccountApiFixture.PostAsync(
            client,
            "/api/account/registrations",
            new { email, password = GoodPassword });

        Assert.NotNull(fixture.Email.LastTo(email));

        var (accountId, token) = fixture.Email.LinkTo(email);

        Assert.NotEqual(Guid.Empty, accountId);
        Assert.NotEmpty(token);
    }

    /// <summary>FR-007: an account starts unconfirmed and cannot be used yet.</summary>
    [Fact]
    public async Task A_new_account_starts_with_its_address_unconfirmed()
    {
        var email = Addresses.Fresh();
        var client = fixture.CreateSeparateBrowser();

        await AccountApiFixture.PostAsync(
            client,
            "/api/account/registrations",
            new { email, password = GoodPassword });

        var confirmed = await fixture.ReadAsync(database =>
            database.Users
                .Where(account => account.Email == email)
                .Select(account => account.EmailConfirmed)
                .SingleAsync());

        Assert.False(confirmed);
    }

    /// <summary>
    /// FR-002: a rejected password names what is wrong, and names it against the
    /// field it concerns, so a screen reader user learns which field to correct
    /// without hunting.
    /// </summary>
    [Fact]
    public async Task A_password_below_the_policy_is_refused_and_the_field_is_named()
    {
        var client = fixture.CreateSeparateBrowser();

        var response = await AccountApiFixture.PostAsync(
            client,
            "/api/account/registrations",
            new { email = Addresses.Fresh(), password = "короткий" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        var errors = problem.GetProperty("errors");

        Assert.True(
            errors.TryGetProperty("password", out var messages),
            "The failure must be tied to the password field, not reported loose.");
        Assert.NotEmpty(messages.EnumerateArray());
    }

    /// <summary>
    /// The spec raises both of these as edge cases. Length is counted over
    /// Unicode, so a password nobody would call short is not called short.
    /// </summary>
    [Theory]
    [InlineData("паролькирилицею")]
    [InlineData("пароль-з-емодзі-🔑🔑")]
    public async Task A_password_that_is_long_enough_is_accepted_whatever_it_is_written_in(
        string password)
    {
        var client = fixture.CreateSeparateBrowser();

        var response = await AccountApiFixture.PostAsync(
            client,
            "/api/account/registrations",
            new { email = Addresses.Fresh(), password });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    /// <summary>An address longer than one can be is refused, not truncated into somebody else's.</summary>
    [Fact]
    public async Task An_address_longer_than_an_address_can_be_is_refused()
    {
        var client = fixture.CreateSeparateBrowser();
        var tooLong = new string('a', 250) + "@example.com";

        var response = await AccountApiFixture.PostAsync(
            client,
            "/api/account/registrations",
            new { email = tooLong, password = GoodPassword });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// FR-006, and the reason the unique index exists. A slow connection makes a
    /// person press the button twice, and both submissions arrive.
    /// </summary>
    [Fact]
    public async Task Submitting_the_same_registration_twice_leaves_one_account()
    {
        var email = Addresses.Fresh();
        var client = fixture.CreateSeparateBrowser();
        var payload = new { email, password = GoodPassword };

        var first = await AccountApiFixture.PostAsync(client, "/api/account/registrations", payload);
        var second = await AccountApiFixture.PostAsync(client, "/api/account/registrations", payload);

        Assert.Equal(HttpStatusCode.Accepted, first.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, second.StatusCode);

        var count = await fixture.ReadAsync(database =>
            database.Users.CountAsync(account => account.Email == email));

        Assert.Equal(1, count);
    }

    /// <summary>
    /// The same thing again, but simultaneously, which is the case the
    /// application's own duplicate check cannot see and only the unique index
    /// settles.
    /// </summary>
    [Fact]
    public async Task Two_simultaneous_registrations_of_one_address_leave_one_account()
    {
        var email = Addresses.Fresh();
        var payload = new { email, password = GoodPassword };

        var browsers = new[] { fixture.CreateSeparateBrowser(), fixture.CreateSeparateBrowser() };

        var responses = await Task.WhenAll(
            browsers.Select(browser =>
                AccountApiFixture.PostAsync(browser, "/api/account/registrations", payload)));

        Assert.All(responses, response =>
            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode));

        var count = await fixture.ReadAsync(database =>
            database.Users.CountAsync(account => account.Email == email));

        Assert.Equal(1, count);
    }
}
