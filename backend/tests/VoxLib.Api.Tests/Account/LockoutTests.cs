using System.Net;
using VoxLib.Api.Tests.Infrastructure;

namespace VoxLib.Api.Tests.Account;

/// <summary>
/// FR-016 and FR-017, and the tension between them and FR-005.
/// <para>
/// FR-016 requires telling a person that they are locked out and when they may
/// try again. A lockout message on its own discloses that the address has an
/// account, which FR-005 forbids. The resolution is that the per-address limit
/// FR-017 already requires produces the same refusal for an address that has no
/// account, so the two are indistinguishable from outside. The second test here
/// is the one holding that down, and it is the reason the limiter is keyed by
/// the submitted address rather than only by the caller.
/// </para>
/// </summary>
[Collection(AccountCollection.Name)]
public class LockoutTests(AccountApiFixture fixture)
{
    [Fact]
    public async Task Repeated_failures_are_refused_and_the_person_is_told_when_to_return()
    {
        var email = await Accounts.ConfirmedAsync(fixture);
        var client = fixture.CreateSeparateBrowser();

        var refused = await ExhaustAsync(client, email);

        Assert.NotNull(refused);
        Assert.Equal(HttpStatusCode.TooManyRequests, refused.StatusCode);

        Assert.True(
            refused.Headers.TryGetValues("Retry-After", out var retryAfter),
            "FR-016: a person who is locked out has to be told when they may try again.");
        Assert.True(int.Parse(retryAfter.Single()) > 0);
    }

    /// <summary>
    /// The same attempts against an address that has no account must produce the
    /// same refusal, or the lockout itself becomes the disclosure.
    /// </summary>
    [Fact]
    public async Task An_address_with_no_account_is_refused_in_exactly_the_same_way()
    {
        var real = await Accounts.ConfirmedAsync(fixture);
        var imaginary = Addresses.Fresh();

        var forReal = await ExhaustAsync(fixture.CreateSeparateBrowser(), real);
        var forImaginary = await ExhaustAsync(fixture.CreateSeparateBrowser(), imaginary);

        Assert.NotNull(forReal);
        Assert.NotNull(forImaginary);

        Assert.Equal(forReal.StatusCode, forImaginary.StatusCode);
        Assert.Equal(await Responses.BodyAsync(forReal), await Responses.BodyAsync(forImaginary));
        Assert.Equal(
            forReal.Headers.GetValues("Retry-After").Single(),
            forImaginary.Headers.GetValues("Retry-After").Single());
    }

    /// <summary>
    /// The correct password does not get through a lockout. Otherwise the
    /// lockout would only slow down a guesser who had already succeeded.
    /// </summary>
    [Fact]
    public async Task The_correct_password_is_refused_while_the_lockout_is_in_force()
    {
        var email = await Accounts.ConfirmedAsync(fixture);
        var client = fixture.CreateSeparateBrowser();

        await ExhaustAsync(client, email);

        var response = await AccountApiFixture.PostAsync(
            client,
            "/api/account/session",
            new { email, password = Accounts.Password });

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
    }

    /// <summary>A lockout is per account, so one person cannot lock out another.</summary>
    [Fact]
    public async Task Locking_one_account_leaves_another_alone()
    {
        var victim = await Accounts.ConfirmedAsync(fixture);
        var bystander = await Accounts.ConfirmedAsync(fixture);

        await ExhaustAsync(fixture.CreateSeparateBrowser(), victim);

        var response = await AccountApiFixture.PostAsync(
            fixture.CreateSeparateBrowser(),
            "/api/account/session",
            new { email = bystander, password = Accounts.Password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Guesses until the answer stops being "not recognised". Deliberately more
    /// attempts than the stated limit, because the two mechanisms that can
    /// refuse here have different thresholds and either one satisfies FR-016.
    /// </summary>
    private static async Task<HttpResponseMessage?> ExhaustAsync(HttpClient client, string email)
    {
        for (var attempt = 0; attempt < 15; attempt++)
        {
            var response = await AccountApiFixture.PostAsync(
                client,
                "/api/account/session",
                new { email, password = $"неправильний пароль {attempt}" });

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                return response;
            }
        }

        return null;
    }
}
