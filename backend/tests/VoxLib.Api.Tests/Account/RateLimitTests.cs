using System.Net;
using System.Net.Http.Json;
using VoxLib.Api.Tests.Infrastructure;

namespace VoxLib.Api.Tests.Account;

/// <summary>
/// FR-017: a limit on how fast one caller may register, sign in, ask for a
/// message or ask for recovery, so that per-account lockout is not the only
/// defence against attempts spread across many addresses.
/// </summary>
[Collection(TightRateLimitCollection.Name)]
public class RateLimitTests(TightRateLimitFixture fixture)
{
    [Fact]
    public async Task A_caller_that_registers_too_fast_is_refused_and_told_when_to_return()
    {
        var client = fixture.CreateClient();

        HttpResponseMessage? refused = null;

        // Comfortably past the limit, because the antiforgery fetch each
        // submission needs counts against the same window.
        for (var attempt = 0; attempt < TightRateLimitFixture.RequestsPerWindow * 3; attempt++)
        {
            var response = await client.PostAsJsonAsync(
                "/api/account/registrations",
                new { email = Addresses.Fresh(), password = "довгий-пароль-2026" });

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                refused = response;
                break;
            }
        }

        Assert.NotNull(refused);
        Assert.True(
            refused.Headers.TryGetValues("Retry-After", out var retryAfter),
            "A refusal has to say when the caller may try again. FR-016 and FR-017.");
        Assert.True(int.Parse(retryAfter.Single()) > 0);
    }
}
