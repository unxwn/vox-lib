namespace VoxLib.Platform.Storage;

/// <summary>
/// Where cover storage is and how to reach it.
/// <para>
/// <see cref="PublicBaseUrl"/> is separate from <see cref="ServiceUrl"/> because
/// in production they are genuinely different addresses: writes go to the R2
/// endpoint with credentials, reads come from a public hostname with none.
/// Collapsing them into one would work in development and quietly hand every
/// visitor a credentialed endpoint in production.
/// </para>
/// </summary>
public sealed class CoverStorageOptions
{
    public const string Section = "Covers";

    /// <summary>The endpoint writes go to. Empty disables storage entirely.</summary>
    public string ServiceUrl { get; set; } = string.Empty;

    public string Bucket { get; set; } = "vox-lib-covers";

    /// <summary>The public address reads come from, with no trailing slash.</summary>
    public string PublicBaseUrl { get; set; } = string.Empty;

    public string AccessKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// True for MinIO, false for R2. Path style puts the bucket in the path
    /// rather than in the hostname, which is what a container-local endpoint
    /// with no wildcard DNS needs.
    /// </summary>
    public bool ForcePathStyle { get; set; }
}
