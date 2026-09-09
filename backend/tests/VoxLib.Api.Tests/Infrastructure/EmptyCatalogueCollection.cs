namespace VoxLib.Api.Tests.Infrastructure;

/// <summary>
/// Separate from the seeded collection because it needs its own database in a
/// different state. Sharing one and emptying it between tests is exactly the
/// cleanup-order flakiness a database per fixture avoids.
/// </summary>
[CollectionDefinition(Name)]
public sealed class EmptyCatalogueCollection : ICollectionFixture<EmptyCatalogueApiFixture>
{
    public const string Name = "empty catalogue";
}
