using VoxLib.Api.Tests.Infrastructure;

namespace VoxLib.Api.Tests.Catalogue;

/// <summary>
/// FR-010: nothing in the catalogue plays, offers or discloses the location of
/// any audio. This is checked against the raw response text rather than against
/// a deserialised shape, because the point is that no such field exists at all,
/// including one nobody thought to model.
/// </summary>
[Collection(CatalogueCollection.Name)]
public class NoAudioExposureTests(CatalogueApiFixture fixture)
{
    /// <summary>
    /// Anything here in a response is either audio or close enough to it to be
    /// worth failing over: a file, a container format, or a way of reaching one.
    /// </summary>
    private static readonly string[] Forbidden =
    [
        "audio",
        "mp3",
        "m4a",
        "m4b",
        "opus",
        "ogg",
        "flac",
        "wav",
        "stream",
        "storagekey",
        "storage_key",
        "signedurl",
        "signed_url",
        "bucket",
        "s3://",
        "blob.core",
    ];

    private readonly HttpClient _client = fixture.CreateClient();

    [Theory]
    [InlineData("/api/books")]
    [InlineData("/api/books?page=2")]
    public async Task No_list_response_mentions_audio_in_any_form(string path)
    {
        var body = await _client.GetStringAsync(path);

        foreach (var token in Forbidden)
        {
            Assert.DoesNotContain(token, body, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// The detail view is the response most likely to grow an audio field, since
    /// it is the page a listener would eventually press play on. Chapters are
    /// listed here by name and running time and by nothing else: a chapter is
    /// where a file reference would naturally be hung, so this is checked per
    /// book rather than on one sample.
    /// </summary>
    [Theory]
    [InlineData("haidamaky")]
    [InlineData("eneida")]
    [InlineData(SeededCatalogue.SlugWithoutChapters)]
    [InlineData(SeededCatalogue.SlugInAnotherLanguage)]
    public async Task No_detail_response_mentions_audio_in_any_form(string slug)
    {
        var body = await _client.GetStringAsync($"/api/books/{slug}");

        foreach (var token in Forbidden)
        {
            Assert.DoesNotContain(token, body, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Sweeps every published book rather than the sample above, so a single
    /// book that acquires an audio field cannot hide behind the others. SC-008
    /// asks for exhaustive rather than sampled.
    /// </summary>
    [Fact]
    public async Task No_published_book_anywhere_in_the_catalogue_mentions_audio()
    {
        foreach (var slug in SeededCatalogue.SlugsInOrder)
        {
            var body = await _client.GetStringAsync($"/api/books/{slug}");

            foreach (var token in Forbidden)
            {
                Assert.DoesNotContain(token, body, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    /// <summary>
    /// Guards the guard. If the seed dataset ever stopped carrying real content
    /// the assertion above would pass against an empty response and prove
    /// nothing.
    /// </summary>
    [Fact]
    public async Task The_list_response_does_carry_content_to_be_checked()
    {
        var body = await _client.GetStringAsync("/api/books");

        Assert.Contains("Ґудзик", body, StringComparison.Ordinal);

        var detail = await _client.GetStringAsync("/api/books/haidamaky");

        Assert.Contains("Гайдамаки", detail, StringComparison.Ordinal);
    }
}
