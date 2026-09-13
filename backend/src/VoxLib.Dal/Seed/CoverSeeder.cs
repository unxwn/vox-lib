using System.Reflection;
using Microsoft.Extensions.Logging;
using VoxLib.Model.Storage;

namespace VoxLib.Dal.Seed;

/// <summary>
/// Puts the committed cover images into object storage.
/// <para>
/// A second class rather than another responsibility on <see cref="CatalogueSeeder"/>,
/// because the two have genuinely different guards. The catalogue seeds only
/// when the database is empty; this one checks <b>per object</b>, because the
/// database and the bucket are separate volumes with separate lifetimes. A
/// container rebuilt with a surviving database and an empty bucket would get no
/// covers at all from a per-run guard, and that is the common case rather than
/// an exotic one (FR-047).
/// </para>
/// <para>
/// It lives in VoxLib.Dal deliberately: <c>ICoverStorage</c> is in VoxLib.Model,
/// which this project may reference, and the seed folder is the catalogue's
/// bootstrap rather than a repository.
/// </para>
/// </summary>
public sealed class CoverSeeder(ICoverStorage storage, ILogger<CoverSeeder> logger)
{
    private const string ResourcePrefix = "VoxLib.Dal.Seed.covers.";

    /// <summary>
    /// Uploads every embedded cover the bucket does not already hold.
    /// <para>
    /// Storage being unreachable is logged and swallowed. The catalogue still
    /// serves, every book shows the placeholder, and the next start tries again;
    /// refusing to start because an image store is down would make covers a
    /// dependency of reading a book's title.
    /// </para>
    /// </summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var assembly = Assembly.GetExecutingAssembly();

        var resources = assembly
            .GetManifestResourceNames()
            .Where(name =>
                name.StartsWith(ResourcePrefix, StringComparison.Ordinal)
                && name.EndsWith(".webp", StringComparison.Ordinal))
            .ToList();

        if (resources.Count == 0)
        {
            logger.LogWarning(
                "No cover images are embedded in the assembly, so none can be uploaded. "
                    + "Check the EmbeddedResource glob in VoxLib.Dal.csproj.");
            return;
        }

        var uploaded = 0;

        foreach (var resource in resources)
        {
            var objectKey = $"covers/{resource[ResourcePrefix.Length..]}";

            try
            {
                if (await storage.ExistsAsync(objectKey, cancellationToken))
                {
                    continue;
                }

                await using var content =
                    assembly.GetManifestResourceStream(resource)
                    ?? throw new InvalidOperationException($"'{resource}' could not be read.");

                await storage.PutAsync(
                    objectKey,
                    content,
                    CoverWidths.ContentType,
                    cancellationToken);

                uploaded++;
            }
            catch (Exception failure) when (failure is not OperationCanceledException)
            {
                logger.LogWarning(
                    failure,
                    "Cover '{ObjectKey}' could not be stored. The catalogue will serve it as "
                        + "the placeholder until storage is reachable.",
                    objectKey);

                // One unreachable store fails every remaining object too, so
                // there is nothing to gain by trying the rest.
                return;
            }
        }

        if (uploaded > 0)
        {
            logger.LogInformation("Stored {Count} cover images.", uploaded);
        }
    }
}
