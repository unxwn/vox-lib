using System.Net;
using Microsoft.EntityFrameworkCore;
using VoxLib.Api.Tests.Infrastructure;
using VoxLib.Dal.Persistence;

namespace VoxLib.Api.Tests.Account;

/// <summary>
/// What happens when the mail provider is unavailable. The specification raises
/// it as an edge case, and it turns out to be a security question rather than
/// only a reliability one.
/// <para>
/// A message goes out only for an address that has an account. So if a failure
/// to send reached the caller, an outage would answer one way for a registered
/// address and another way for an unregistered one, which is exactly the
/// disclosure FR-005 forbids. It would appear the moment the provider had a bad
/// day, and in no test written against a working one.
/// </para>
/// </summary>
[Collection(BrokenMailCollection.Name)]
public class MailOutageTests(BrokenMailFixture fixture)
{
    private const string Password = "довгий-пароль-2026";

    /// <summary>
    /// The important one. A known address and an unknown address must still be
    /// answered identically while mail is down.
    /// </summary>
    [Fact]
    public async Task Recovery_answers_the_same_for_a_known_and_an_unknown_address_while_mail_is_down()
    {
        var known = Addresses.Fresh();
        var client = fixture.CreateClient();

        await AccountApiFixture.PostAsync(
            client,
            "/api/account/registrations",
            new { email = known, password = Password });

        var forKnown = await AccountApiFixture.PostAsync(
            client,
            "/api/account/recovery-requests",
            new { email = known });

        var forUnknown = await AccountApiFixture.PostAsync(
            client,
            "/api/account/recovery-requests",
            new { email = Addresses.Fresh() });

        Assert.Equal(HttpStatusCode.Accepted, forKnown.StatusCode);
        Assert.Equal(forKnown.StatusCode, forUnknown.StatusCode);
        Assert.Equal(await Responses.BodyAsync(forKnown), await Responses.BodyAsync(forUnknown));
    }

    /// <summary>The same for asking again for a confirmation message.</summary>
    [Fact]
    public async Task Resending_confirmation_answers_the_same_for_both_while_mail_is_down()
    {
        var known = Addresses.Fresh();
        var client = fixture.CreateClient();

        await AccountApiFixture.PostAsync(
            client,
            "/api/account/registrations",
            new { email = known, password = Password });

        var forKnown = await AccountApiFixture.PostAsync(
            client,
            "/api/account/confirmation-requests",
            new { email = known });

        var forUnknown = await AccountApiFixture.PostAsync(
            client,
            "/api/account/confirmation-requests",
            new { email = Addresses.Fresh() });

        Assert.Equal(HttpStatusCode.Accepted, forKnown.StatusCode);
        Assert.Equal(forKnown.StatusCode, forUnknown.StatusCode);
    }

    /// <summary>
    /// And the account survives. Registering creates it before it sends
    /// anything, so a failure that propagated would leave a person with an
    /// account they cannot confirm and a message telling them they have none.
    /// </summary>
    [Fact]
    public async Task Registering_still_creates_the_account_when_the_message_cannot_be_sent()
    {
        var email = Addresses.Fresh();
        var client = fixture.CreateClient();

        var response = await AccountApiFixture.PostAsync(
            client,
            "/api/account/registrations",
            new { email, password = Password });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        await using var database = new VoxLibDbContext(
            new DbContextOptionsBuilder<VoxLibDbContext>()
                .UseNpgsql(fixture.ConnectionString)
                .Options);

        Assert.Equal(1, await database.Users.CountAsync(account => account.Email == email));
    }

    /// <summary>
    /// And the person can still ask again once the provider recovers, which is
    /// what makes swallowing the failure recoverable rather than merely quiet.
    /// </summary>
    [Fact]
    public async Task Asking_for_the_message_again_is_still_accepted_while_mail_is_down()
    {
        var email = Addresses.Fresh();
        var client = fixture.CreateClient();

        await AccountApiFixture.PostAsync(
            client,
            "/api/account/registrations",
            new { email, password = Password });

        var again = await AccountApiFixture.PostAsync(
            client,
            "/api/account/confirmation-requests",
            new { email });

        Assert.Equal(HttpStatusCode.Accepted, again.StatusCode);
    }
}
