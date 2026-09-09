namespace VoxLib.Api.Tests.Infrastructure;

/// <summary>
/// Every test against the seeded catalogue shares one application and one
/// database. The tests only read, so sharing is safe, and it means one database
/// is created and dropped per run rather than one per test class.
/// </summary>
[CollectionDefinition(Name)]
public sealed class CatalogueCollection : ICollectionFixture<CatalogueApiFixture>
{
    public const string Name = "catalogue";
}
