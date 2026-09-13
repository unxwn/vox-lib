namespace VoxLib.Model.Storage;

/// <summary>
/// Where cover images live. Implemented once, in VoxLib.Platform, against
/// S3-compatible storage: MinIO in development, Cloudflare R2 in production,
/// with nothing but configuration between them.
/// <para>
/// Three members, and <b>the absences are the contract</b>. There is no signing
/// method, so this interface cannot express a private object at all; no delete
/// and no list, so nothing here can remove or enumerate a cover; no bucket
/// administration, because <c>PutBucketPolicy</c> works on MinIO and is not
/// supported by R2, and an implementation that set the policy itself would carry
/// a development-only branch inside the production adapter. And no
/// <c>GetAsync</c>, because the API never serves cover bytes — the browser
/// fetches them from storage directly.
/// </para>
/// <para>
/// <c>IAudioStorage</c> is a separate interface that does not exist yet, and it
/// is deliberately not a generalisation of this one: covers are public and
/// cacheable, audio is signed and must never be cached, and one interface
/// serving both would have to be told on every call which it was holding.
/// </para>
/// </summary>
public interface ICoverStorage
{
    /// <summary>
    /// The public, unsigned, cacheable address of one prepared width.
    /// <para>
    /// Pure composition from configuration: it touches no network and cannot
    /// fail, which is what keeps a catalogue response from ever failing because
    /// storage is unreachable (FR-051).
    /// </para>
    /// </summary>
    Uri PublicUrl(string coverKey, int width);

    /// <summary>
    /// Whether an object is already stored. Used by seeding, never by a read
    /// path.
    /// </summary>
    Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken);

    /// <summary>
    /// Stores one prepared image. Used by seeding, never by a read path.
    /// </summary>
    Task PutAsync(
        string objectKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken);
}
