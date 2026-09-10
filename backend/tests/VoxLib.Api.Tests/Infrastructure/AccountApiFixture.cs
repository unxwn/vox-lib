using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing.Handlers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VoxLib.Dal.Persistence;
using VoxLib.Model.Account;
using VoxLib.Model.Messaging;

namespace VoxLib.Api.Tests.Infrastructure;

/// <summary>
/// The application with somewhere for mail to go, and with one cookie jar shared
/// between clients.
/// <para>
/// The shared jar is the point. Building a fresh client over the same jar is
/// what "closed the browser and reopened it" means to a test, which is the whole
/// of FR-012 and SC-004. The default client would get its own jar and every such
/// test would pass for the wrong reason.
/// </para>
/// </summary>
public sealed class AccountApiFixture : ApiFixture
{
    public RecordingEmailSender Email { get; } = new();

    /// <summary>
    /// One jar, so a client thrown away and rebuilt is the same browser. Reset
    /// it with <see cref="NewBrowser"/> when a test needs a different one.
    /// </summary>
    public CookieContainer Cookies { get; private set; } = new();

    protected override void ConfigureHost(IWebHostBuilder builder)
    {
        // A whole run shares one client address, so a limit sized for a real
        // caller would refuse the tests rather than an attack. The test that is
        // about the limit sets its own.
        builder.UseSetting("RateLimit:RequestsPerClientWindow", "100000");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(Email);
        });
    }

    /// <summary>A client over the shared jar: the same browser as every other one.</summary>
    public HttpClient CreateBrowser() =>
        Factory.CreateDefaultClient(new CookieContainerHandler(Cookies));

    /// <summary>A client with its own jar: a different device.</summary>
    public HttpClient CreateSeparateBrowser() => CreateBrowserWithJar().Client;

    /// <summary>
    /// A client with its own jar, and the jar, for the tests that have to look
    /// at the session cookie itself rather than at what it does.
    /// </summary>
    public (HttpClient Client, CookieContainer Jar) CreateBrowserWithJar()
    {
        var jar = new CookieContainer();

        return (Factory.CreateDefaultClient(new CookieContainerHandler(jar)), jar);
    }

    /// <summary>Forgets every cookie, as clearing site data would.</summary>
    public void NewBrowser() => Cookies = new CookieContainer();

    /// <summary>
    /// Posts as a browser does: with the session cookie, and with the
    /// antiforgery token fetched first and echoed in the header. Every
    /// state-changing endpoint requires it, so a test that skipped it would be
    /// testing the guard rather than the behaviour.
    /// </summary>
    public static async Task<HttpResponseMessage> PostAsync(
        HttpClient client,
        string path,
        object? payload = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path);

        if (payload is not null)
        {
            request.Content = JsonContent.Create(payload);
        }

        return await SendWithTokenAsync(client, request);
    }

    public static async Task<HttpResponseMessage> DeleteAsync(HttpClient client, string path) =>
        await SendWithTokenAsync(client, new HttpRequestMessage(HttpMethod.Delete, path));

    private static async Task<HttpResponseMessage> SendWithTokenAsync(
        HttpClient client,
        HttpRequestMessage request)
    {
        var token = await TokenAsync(client);

        request.Headers.Add("X-XSRF-TOKEN", token);

        return await client.SendAsync(request);
    }

    /// <summary>
    /// The antiforgery token for this browser, read back out of the readable
    /// cookie exactly as a page would read it.
    /// </summary>
    public static async Task<string> TokenAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/account/antiforgery-token");

        response.EnsureSuccessStatusCode();

        var cookies = response.Headers
            .Where(header => header.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase))
            .SelectMany(header => header.Value)
            .ToList();

        var token = cookies
            .Select(cookie => cookie.Split(';')[0])
            .FirstOrDefault(cookie => cookie.StartsWith("XSRF-TOKEN=", StringComparison.Ordinal));

        return token is null
            ? throw new InvalidOperationException(
                "The antiforgery endpoint did not set a readable XSRF-TOKEN cookie.")
            : Uri.UnescapeDataString(token["XSRF-TOKEN=".Length..]);
    }

    /// <summary>
    /// Ends every session for an account from the server, as FR-014 requires be
    /// possible. There is no screen for it, by decision, so a test reaches the
    /// store the way the product would.
    /// </summary>
    public async Task EndAllSessionsAsync(string email)
    {
        await using var scope = Factory.Services.CreateAsyncScope();

        var store = scope.ServiceProvider.GetRequiredService<IAccountStore>();

        var account = await store.FindAsync(email, CancellationToken.None)
            ?? throw new InvalidOperationException($"No account for {email}.");

        await store.EndAllSessionsAsync(account.Id, CancellationToken.None);
    }

    /// <summary>Reads the database directly, for the few assertions that are about storage.</summary>
    public async Task<T> ReadAsync<T>(Func<VoxLibDbContext, Task<T>> read)
    {
        await using var database = new VoxLibDbContext(
            new DbContextOptionsBuilder<VoxLibDbContext>().UseNpgsql(ConnectionString).Options);

        return await read(database);
    }
}
