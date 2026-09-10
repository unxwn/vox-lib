using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using VoxLib.Api.Tests.Infrastructure;

namespace VoxLib.Api.Tests.Account;

/// <summary>
/// Getting an account into the state a test needs, through the product's own
/// endpoints rather than by writing rows.
/// <para>
/// Going through the endpoints is deliberate: it means a test cannot arrange a
/// state the product itself cannot reach, which is the usual way a suite ends up
/// proving something about a database instead of about a person.
/// </para>
/// </summary>
internal static class Accounts
{
    public const string Password = "довгий-пароль-2026";

    /// <summary>An account that exists but whose address is not confirmed.</summary>
    public static async Task<string> RegisteredAsync(AccountApiFixture fixture, string? email = null)
    {
        email ??= Addresses.Fresh();

        var client = fixture.CreateSeparateBrowser();

        var response = await AccountApiFixture.PostAsync(
            client,
            "/api/account/registrations",
            new { email, password = Password });

        response.EnsureSuccessStatusCode();

        return email;
    }

    /// <summary>An account that can be signed in to.</summary>
    public static async Task<string> ConfirmedAsync(AccountApiFixture fixture, string? email = null)
    {
        email = await RegisteredAsync(fixture, email);

        var client = fixture.CreateSeparateBrowser();
        var (accountId, token) = fixture.Email.LinkTo(email);

        var response = await AccountApiFixture.PostAsync(
            client,
            "/api/account/confirmations",
            new { accountId, token });

        response.EnsureSuccessStatusCode();

        return email;
    }

    /// <summary>A browser already signed in to a fresh confirmed account.</summary>
    public static async Task<(HttpClient Client, string Email)> SignedInAsync(
        AccountApiFixture fixture)
    {
        var email = await ConfirmedAsync(fixture);
        var client = fixture.CreateSeparateBrowser();

        var response = await AccountApiFixture.PostAsync(
            client,
            "/api/account/session",
            new { email, password = Password });

        response.EnsureSuccessStatusCode();

        return (client, email);
    }

    public static async Task<SessionBody> ReadSessionAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/account/session");

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<SessionBody>(
            new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("The session endpoint returned no body.");
    }

    internal sealed record SessionBody(
        [property: JsonPropertyName("signedIn")] bool SignedIn,
        [property: JsonPropertyName("email")] string? Email,
        [property: JsonPropertyName("isVerifiedBeneficiary")] bool IsVerifiedBeneficiary);
}
