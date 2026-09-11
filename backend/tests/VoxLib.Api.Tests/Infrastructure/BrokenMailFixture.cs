using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VoxLib.Model.Messaging;

namespace VoxLib.Api.Tests.Infrastructure;

/// <summary>
/// The application with no working mail provider, which the specification raises
/// as an edge case and which is the state a real outage puts it in.
/// </summary>
public sealed class BrokenMailFixture : ApiFixture
{
    protected override void ConfigureHost(IWebHostBuilder builder)
    {
        builder.UseSetting("RateLimit:RequestsPerClientWindow", "100000");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender, FailingEmailSender>();
        });
    }
}

[CollectionDefinition(Name)]
public sealed class BrokenMailCollection : ICollectionFixture<BrokenMailFixture>
{
    public const string Name = "broken-mail";
}
