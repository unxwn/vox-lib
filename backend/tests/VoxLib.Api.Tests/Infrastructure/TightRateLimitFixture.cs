using Microsoft.AspNetCore.Hosting;

namespace VoxLib.Api.Tests.Infrastructure;

/// <summary>
/// The application with the client limit set low enough to reach.
/// <para>
/// A separate fixture, and therefore a separate database, because the shared one
/// deliberately raises the limit out of the way: a whole test run comes from one
/// client address, so a limit sized for a real caller would refuse the tests
/// rather than an attack.
/// </para>
/// </summary>
public sealed class TightRateLimitFixture : ApiFixture
{
    public const int RequestsPerWindow = 5;

    protected override void ConfigureHost(IWebHostBuilder builder) =>
        builder.UseSetting(
            "RateLimit:RequestsPerClientWindow",
            RequestsPerWindow.ToString());
}

[CollectionDefinition(Name)]
public sealed class TightRateLimitCollection : ICollectionFixture<TightRateLimitFixture>
{
    public const string Name = "tight-rate-limit";
}
