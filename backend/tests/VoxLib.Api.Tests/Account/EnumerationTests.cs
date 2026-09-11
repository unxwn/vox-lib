using System.Diagnostics;
using System.Net;
using VoxLib.Api.Tests.Infrastructure;

namespace VoxLib.Api.Tests.Account;

/// <summary>
/// FR-005 and SC-007: no response, and no measurable difference in the time
/// taken to produce one, reveals whether an email address has an account.
/// <para>
/// This is the file that stops the product from being a way to find out who has
/// registered. It is worth reading before changing any wording in the account
/// endpoints, because a difference of one word between two branches is enough to
/// break it.
/// </para>
/// </summary>
[Collection(AccountCollection.Name)]
public class EnumerationTests(AccountApiFixture fixture)
{
    private const string Password = "довгий-пароль-2026";

    /// <summary>
    /// Registering an address that already has an account must be
    /// indistinguishable from registering a new one. The person who owns the
    /// address is told, in a message to that address, which is where telling
    /// them is safe.
    /// </summary>
    [Fact]
    public async Task Registering_a_known_address_answers_exactly_as_a_new_one_does()
    {
        var taken = Addresses.Fresh();
        var client = fixture.CreateSeparateBrowser();

        await AccountApiFixture.PostAsync(
            client,
            "/api/account/registrations",
            new { email = taken, password = Password });

        var again = await AccountApiFixture.PostAsync(
            client,
            "/api/account/registrations",
            new { email = taken, password = Password });

        var fresh = await AccountApiFixture.PostAsync(
            client,
            "/api/account/registrations",
            new { email = Addresses.Fresh(), password = Password });

        Assert.Equal(fresh.StatusCode, again.StatusCode);
        Assert.Equal(await Responses.BodyAsync(fresh), await Responses.BodyAsync(again));
        Assert.Equal(Shape(fresh), Shape(again));
    }

    /// <summary>
    /// The person who owns an address that is registered again is told so, and
    /// offered recovery. This is what makes the identical response above humane
    /// rather than merely safe.
    /// </summary>
    [Fact]
    public async Task The_owner_of_an_address_registered_again_is_told_in_a_message()
    {
        var taken = Addresses.Fresh();
        var client = fixture.CreateSeparateBrowser();

        await AccountApiFixture.PostAsync(
            client,
            "/api/account/registrations",
            new { email = taken, password = Password });

        var before = fixture.Email.CountTo(taken);

        await AccountApiFixture.PostAsync(
            client,
            "/api/account/registrations",
            new { email = taken, password = Password });

        Assert.Equal(before + 1, fixture.Email.CountTo(taken));
    }

    /// <summary>FR-015: one answer for an unknown address and for a wrong password.</summary>
    [Fact]
    public async Task An_unknown_address_and_a_wrong_password_answer_identically()
    {
        var known = await RegisterAndConfirmAsync();
        var client = fixture.CreateSeparateBrowser();

        var wrongPassword = await AccountApiFixture.PostAsync(
            client,
            "/api/account/session",
            new { email = known, password = "зовсім не той пароль" });

        var unknownAddress = await AccountApiFixture.PostAsync(
            client,
            "/api/account/session",
            new { email = Addresses.Fresh(), password = "зовсім не той пароль" });

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(unknownAddress.StatusCode, wrongPassword.StatusCode);
        Assert.Equal(await Responses.BodyAsync(unknownAddress), await Responses.BodyAsync(wrongPassword));
    }

    /// <summary>
    /// Requesting recovery for an address with no account must look like
    /// requesting it for one that has.
    /// </summary>
    [Fact]
    public async Task Requesting_recovery_answers_the_same_whether_or_not_the_address_is_known()
    {
        var known = await RegisterAndConfirmAsync();
        var client = fixture.CreateSeparateBrowser();

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

    /// <summary>
    /// SC-007's timing half. An unknown address must not return before any
    /// hashing has happened, which is what
    /// <c>BurnPasswordVerificationAsync</c> exists to prevent.
    /// <para>
    /// Wall-clock timing on a shared machine is noisy, so this asserts something
    /// deliberately loose: the two medians are within a factor of four. That is
    /// far too coarse to catch a subtle leak and quite tight enough to catch the
    /// real regression, which is returning early and being an order of magnitude
    /// faster.
    /// </para>
    /// </summary>
    [Fact]
    public async Task An_unknown_address_takes_comparable_time_to_a_wrong_password()
    {
        var known = await RegisterAndConfirmAsync();
        var client = fixture.CreateSeparateBrowser();

        var knownTimes = new List<double>();
        var unknownTimes = new List<double>();

        for (var attempt = 0; attempt < 7; attempt++)
        {
            knownTimes.Add(await TimeAsync(client, known));
            unknownTimes.Add(await TimeAsync(client, Addresses.Fresh()));
        }

        var knownMedian = Median(knownTimes);
        var unknownMedian = Median(unknownTimes);

        Assert.True(
            unknownMedian * 4 > knownMedian && knownMedian * 4 > unknownMedian,
            $"An unknown address took {unknownMedian:F1} ms and a wrong password "
                + $"{knownMedian:F1} ms. One returning far sooner than the other is how "
                + "the response time tells somebody that an address has an account.");
    }

    private async Task<double> TimeAsync(HttpClient client, string email)
    {
        var stopwatch = Stopwatch.StartNew();

        using var response = await AccountApiFixture.PostAsync(
            client,
            "/api/account/session",
            new { email, password = "зовсім не той пароль" });

        return stopwatch.Elapsed.TotalMilliseconds;
    }

    private static double Median(List<double> values)
    {
        var sorted = values.Order().ToList();

        return sorted[sorted.Count / 2];
    }

    private async Task<string> RegisterAndConfirmAsync()
    {
        var email = Addresses.Fresh();
        var client = fixture.CreateSeparateBrowser();

        await AccountApiFixture.PostAsync(
            client,
            "/api/account/registrations",
            new { email, password = Password });

        var (accountId, token) = fixture.Email.LinkTo(email);

        await AccountApiFixture.PostAsync(
            client,
            "/api/account/confirmations",
            new { accountId, token });

        return email;
    }

    /// <summary>
    /// Everything about the response except the parts that legitimately differ
    /// between two requests: the date, and the cookies the antiforgery service
    /// happens to refresh.
    /// </summary>
    private static string Shape(HttpResponseMessage response) =>
        string.Join(
            "|",
            response.Headers
                .Where(header =>
                    !header.Key.Equals("Date", StringComparison.OrdinalIgnoreCase)
                    && !header.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase))
                .OrderBy(header => header.Key, StringComparer.Ordinal)
                .Select(header => $"{header.Key}={string.Join(",", header.Value)}"));
}
