using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using VoxLib.Dal.Persistence;

namespace VoxLib.Api.Tests.Infrastructure;

/// <summary>
/// Boots the real application over real HTTP against a real PostgreSQL, which is
/// the only way the behaviours that matter here can be verified: ordering under
/// Ukrainian collation and case-insensitive matching are database behaviours, so
/// an in-memory provider would test something the product does not do.
/// <para>
/// Each run gets its own database, so runs in parallel cannot collide and the
/// development data is neither depended on nor disturbed. The application's own
/// startup applies the migrations and seeds the catalogue, so this harness
/// exercises that path rather than reimplementing it.
/// </para>
/// </summary>
public abstract class ApiFixture : IAsyncLifetime
{
    private WebApplicationFactory<Program>? _factory;

    protected ApiFixture()
    {
        ConnectionString = new NpgsqlConnectionStringBuilder(DatabaseConnection.Resolve())
        {
            Database = $"voxlib_test_{Guid.NewGuid():N}",
        }.ConnectionString;
    }

    public string ConnectionString { get; }

    public HttpClient CreateClient() => Factory.CreateClient();

    protected WebApplicationFactory<Program> Factory =>
        _factory ?? throw new InvalidOperationException("The fixture has not started.");

    public async Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting(DatabaseConnection.EnvironmentVariable, ConnectionString);
            ConfigureHost(builder);
        });

        // The host is built lazily, and building it is what creates the database,
        // migrates it and seeds it. Ask for a client so that has all happened
        // before the first test runs.
        using var warmUp = _factory.CreateClient();
        await warmUp.GetAsync("/health");

        await using var scope = _factory.Services.CreateAsyncScope();
        await PrepareAsync(scope.ServiceProvider.GetRequiredService<VoxLibDbContext>());
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        // Npgsql pools connections per connection string, and PostgreSQL refuses
        // to drop a database that still has one open.
        NpgsqlConnection.ClearAllPools();

        await using var database = new VoxLibDbContext(
            new DbContextOptionsBuilder<VoxLibDbContext>().UseNpgsql(ConnectionString).Options);

        await database.Database.EnsureDeletedAsync();
    }

    /// <summary>
    /// Runs once, after the application has started and seeded, for a fixture
    /// that needs the catalogue in some other state.
    /// </summary>
    protected virtual Task PrepareAsync(VoxLibDbContext database) => Task.CompletedTask;

    /// <summary>
    /// For a fixture that has to change a setting or replace a service before
    /// the application starts. The accounts harness uses it to substitute the
    /// mail sender, which is the one boundary that cannot be exercised for real
    /// in a test.
    /// </summary>
    protected virtual void ConfigureHost(IWebHostBuilder builder)
    {
    }
}
