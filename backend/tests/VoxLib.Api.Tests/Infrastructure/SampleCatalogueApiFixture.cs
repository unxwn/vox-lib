using VoxLib.Dal.Persistence;

namespace VoxLib.Api.Tests.Infrastructure;

/// <summary>
/// The application over <see cref="SampleCatalogue"/>: a catalogue large and
/// varied enough to page, to sort across the awkward Ukrainian letters, and to
/// hold a draft and several books missing optional metadata.
/// </summary>
public sealed class SampleCatalogueApiFixture : ApiFixture
{
    protected override Task PrepareAsync(VoxLibDbContext database) =>
        SampleCatalogue.ReplaceCatalogueAsync(database);
}
