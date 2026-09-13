namespace VoxLib.Api.Tests.Infrastructure;

/// <summary>
/// One application and one database for every test reading the sample
/// catalogue, so the fixture is built once rather than per class.
/// </summary>
[CollectionDefinition(Name)]
public sealed class SampleCatalogueCollection : ICollectionFixture<SampleCatalogueApiFixture>
{
    public const string Name = "sample catalogue";
}
