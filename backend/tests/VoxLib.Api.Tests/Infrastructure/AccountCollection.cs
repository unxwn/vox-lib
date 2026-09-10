namespace VoxLib.Api.Tests.Infrastructure;

/// <summary>
/// Every account test shares one application and one database. Unlike the
/// catalogue tests these write, so each one uses its own email address rather
/// than relying on an empty table.
/// </summary>
[CollectionDefinition(Name)]
public sealed class AccountCollection : ICollectionFixture<AccountApiFixture>
{
    public const string Name = "accounts";
}
