using Microsoft.EntityFrameworkCore;
using VoxLib.Api.Book;
using VoxLib.Api.OpenApi;
using VoxLib.Dal.Book;
using VoxLib.Dal.Persistence;
using VoxLib.Dal.Seed;
using VoxLib.Model.Book;
using VoxLib.Orchestrator.Book;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi(options =>
    options.AddSchemaTransformer<IntegerSchemaTransformer>());
builder.Services.AddProblemDetails();

// This file is the composition root, and the only place in VoxLib.Api that is
// allowed to name VoxLib.Dal. Endpoints depend on the interfaces in
// VoxLib.Model, so the database stays a detail at the edge.
//
// The connection string is read through configuration rather than straight off
// the environment. Environment variables are already a configuration source, and
// going through it is what lets the test harness point the application at its
// own database.
var connectionString =
    builder.Configuration[DatabaseConnection.EnvironmentVariable] is { Length: > 0 } configured
        ? configured
        : DatabaseConnection.LocalDefault;

builder.Services.AddDbContext<VoxLibDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<IBookRepository, BookRepository>();
builder.Services.AddScoped<IBookCatalogue, BookCatalogue>();
builder.Services.AddScoped<CatalogueSeeder>();

// The Vite dev server proxies /api through itself, so CORS is not needed for the
// proxied path. It is here for the case where the browser calls the API directly.
const string DevCors = "dev-frontend";
builder.Services.AddCors(options =>
{
    options.AddPolicy(DevCors, policy => policy
        .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

var app = builder.Build();

// Bring the schema up to date and populate an empty catalogue before serving.
// Migrating at startup is convenient while one instance runs; it is normally
// replaced by an explicit deployment step once more than one does, and that is
// worth revisiting before the first real deployment.
await using (var startup = app.Services.CreateAsyncScope())
{
    await startup.ServiceProvider.GetRequiredService<VoxLibDbContext>().Database.MigrateAsync();
    await startup.ServiceProvider.GetRequiredService<CatalogueSeeder>().SeedAsync();
}

// A query parameter that will not parse is the caller's mistake, and the
// framework already says so by throwing BadHttpRequestException with a 400 on
// it. Without this selector the handler reports every exception as a 500, so
// "?page=abc" would come back as a server fault.
app.UseExceptionHandler(new ExceptionHandlerOptions
{
    StatusCodeSelector = exception =>
        exception is BadHttpRequestException badRequest
            ? badRequest.StatusCode
            : StatusCodes.Status500InternalServerError,
});
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();          // -> /openapi/v1.json
    app.UseCors(DevCors);
}
else
{
    // Only redirect in non-dev: the container's http profile has no https port,
    // and enabling it there produces "failed to determine the https port" warnings.
    app.UseHttpsRedirection();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok", utc = DateTimeOffset.UtcNow }))
   .WithName("Health");

app.MapBookEndpoints();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/api/weatherforecast", () =>
        Enumerable.Range(1, 5).Select(index =>
            new WeatherForecast(
                DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                Random.Shared.Next(-20, 55),
                summaries[Random.Shared.Next(summaries.Length)]))
            .ToArray())
   .WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

// Exposes the implicit Program class generated from top-level statements so that
// WebApplicationFactory<Program> in the test project can boot the real app.
public partial class Program;
