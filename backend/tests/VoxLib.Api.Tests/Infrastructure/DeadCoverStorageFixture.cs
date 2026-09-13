using Microsoft.AspNetCore.Hosting;

namespace VoxLib.Api.Tests.Infrastructure;

/// <summary>
/// The application with cover storage pointed at an endpoint nothing answers on.
/// <para>
/// A dead endpoint rather than a substituted service, deliberately: the point is
/// that the real adapter, against real storage that is down, cannot take the
/// catalogue with it. A fake that returned URLs would prove only that the fake
/// works.
/// </para>
/// </summary>
public sealed class DeadCoverStorageFixture : ApiFixture
{
    /// <summary>A port nothing is listening on, in the ephemeral range.</summary>
    public const string DeadEndpoint = "http://localhost:9";

    protected override void ConfigureHost(IWebHostBuilder builder) =>
        builder.UseSetting("Covers:ServiceUrl", DeadEndpoint);
}
