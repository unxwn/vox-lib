using VoxLib.Api.Tests.Infrastructure;

namespace VoxLib.Api.Tests.Account;

/// <summary>
/// This feature exists so that the audio feature has something to authorise
/// against. It never plays, offers, or reveals the location of audio, and that
/// is asserted rather than assumed, exactly as the catalogue asserts it.
/// </summary>
[Collection(AccountCollection.Name)]
public class NoAudioExposureTests(AccountApiFixture fixture)
{
    /// <summary>
    /// Words that would mean audio had leaked into a response about an account.
    /// Note what is absent: a bare "signed", because the session response says
    /// signedIn and matching that would be matching the wrong thing. What is
    /// being looked for is a signed URL, which is what a leak would actually
    /// look like.
    /// </summary>
    private static readonly string[] Forbidden =
    [
        "audio", "mp3", "opus", "m4b", "storage", "bucket", "stream",
        "signedurl", "signed_url", "signed-url",
    ];

    [Theory]
    [InlineData("/api/account/session")]
    [InlineData("/api/account/password-policy")]
    public async Task No_account_response_mentions_audio_or_where_it_is_kept(string path)
    {
        var client = fixture.CreateSeparateBrowser();

        var body = await (await client.GetAsync(path)).Content.ReadAsStringAsync();

        foreach (var word in Forbidden)
        {
            Assert.DoesNotContain(word, body, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// And a signed-in session says no more than an anonymous one does. Signing
    /// in is the moment a careless implementation starts handing out things a
    /// listener "will need".
    /// </summary>
    [Fact]
    public async Task A_signed_in_session_reveals_nothing_about_audio_either()
    {
        var (client, _) = await Accounts.SignedInAsync(fixture);

        var body = await (await client.GetAsync("/api/account/session")).Content.ReadAsStringAsync();

        foreach (var word in Forbidden)
        {
            Assert.DoesNotContain(word, body, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Neither does a message. A confirmation link goes to an address that has
    /// not been proved yet, so anything in it is effectively public.
    /// </summary>
    [Fact]
    public async Task No_message_mentions_audio()
    {
        var email = await Accounts.RegisteredAsync(fixture);

        var message = fixture.Email.LastTo(email);

        Assert.NotNull(message);

        foreach (var word in Forbidden)
        {
            Assert.DoesNotContain(word, message.PlainTextBody, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(word, message.HtmlBody, StringComparison.OrdinalIgnoreCase);
        }
    }
}
