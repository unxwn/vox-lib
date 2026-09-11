namespace VoxLib.Api.Tests.Account;

/// <summary>
/// Account tests write, and they share one database, so each one needs an
/// address no other test has used.
/// </summary>
internal static class Addresses
{
    public static string Fresh(string prefix = "listener") =>
        $"{prefix}-{Guid.NewGuid():N}@example.com";
}
