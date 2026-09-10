using VoxLib.Api.Tests.Infrastructure;

namespace VoxLib.Api.Tests.Account;

/// <summary>
/// FR-028 and Principle IV of the constitution: pages belonging to an account
/// stay out of search indexes, and robots.txt is advisory and is never the
/// protection.
/// </summary>
[Collection(AccountCollection.Name)]
public class AccountHeaderTests(AccountApiFixture fixture)
{
    [Theory]
    [InlineData("/api/account/session")]
    [InlineData("/api/account/password-policy")]
    [InlineData("/api/account/antiforgery-token")]
    public async Task Every_account_response_says_it_is_not_for_indexing(string path)
    {
        var client = fixture.CreateSeparateBrowser();

        var response = await client.GetAsync(path);

        Assert.True(
            response.Headers.TryGetValues("X-Robots-Tag", out var values),
            $"{path} did not carry X-Robots-Tag.");
        Assert.Contains("noindex", values);
    }

    /// <summary>
    /// A refusal is still a page about an account, so it carries the header too.
    /// The middleware is on the path rather than on the successful branch.
    /// </summary>
    [Fact]
    public async Task A_refused_request_carries_the_header_as_well()
    {
        var client = fixture.CreateSeparateBrowser();

        var response = await client.PostAsync("/api/account/registrations", null);

        Assert.True(response.Headers.TryGetValues("X-Robots-Tag", out var values));
        Assert.Contains("noindex", values);
    }

    /// <summary>
    /// And the catalogue does not, because it is public and indexable by design.
    /// Principle IV again, from the other side.
    /// </summary>
    [Fact]
    public async Task The_catalogue_is_still_indexable()
    {
        var client = fixture.CreateSeparateBrowser();

        var response = await client.GetAsync("/api/books");

        Assert.False(response.Headers.TryGetValues("X-Robots-Tag", out _));
    }
}
