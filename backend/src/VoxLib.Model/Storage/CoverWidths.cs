namespace VoxLib.Model.Storage;

/// <summary>
/// The widths every cover is prepared at, and the key those prepared objects
/// share.
/// <para>
/// Two widths rather than three. FR-049 renders the catalogue cover at 128 and
/// the book page at 192 at a 1280 viewport, so 160 serves 128 at 1× and 320
/// serves 128 at 2× and 192 at 1.67×. A 640 was planned and dropped because
/// three of the four source images are 507, 517 and 550 pixels wide, so it would
/// have existed only by upscaling. The third width returns when a source wide
/// enough to justify it does.
/// </para>
/// <para>
/// It lives in the domain rather than in the adapter because both the seeder and
/// the response composition have to agree on it, and they are in different
/// projects.
/// </para>
/// </summary>
public static class CoverWidths
{
    public static readonly IReadOnlyList<int> Prepared = [160, 320];

    public const string ContentType = "image/webp";

    /// <summary>
    /// The object key one prepared width lives at. The stem is what a book's
    /// row stores; this is the only place the two are combined.
    /// </summary>
    public static string ObjectKey(string coverKey, int width) =>
        $"covers/{coverKey}-{width}.webp";
}
