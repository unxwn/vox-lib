using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VoxLib.Api.Account;
using VoxLib.Api.Book;
using VoxLib.Api.OpenApi;
using VoxLib.Dal.Account;
using VoxLib.Dal.Book;
using VoxLib.Dal.Persistence;
using VoxLib.Dal.Seed;
using VoxLib.Model.Account;
using VoxLib.Model.Book;
using VoxLib.Model.Messaging;
using VoxLib.Orchestrator.Account;
using VoxLib.Orchestrator.Book;
using VoxLib.Platform.Email;

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

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHttpContextAccessor();

// The identity component supplies four things and no more: the user store, the
// password hasher, the lockout counter and the link tokens. Not its screens,
// which are not Ukrainian and do not meet the accessibility bar this product is
// held to, and not MapIdentityApi, which returns bearer tokens rather than a
// cookie session and owns wording that several requirements are statements
// about. Hence AddIdentityCore rather than AddIdentity: the role services and
// their three tables would never be read, because permission here is decided
// from a claim.
builder.Services
    .AddIdentityCore<AccountDao>(options =>
    {
        options.User.RequireUniqueEmail = true;

        // FR-002. Length alone, well above the component's default of six.
        // Character-class rules push people toward short passwords full of
        // substitutions and are painful to type on a phone with a screen reader.
        options.Password.RequiredLength = PasswordPolicy.MinimumLength;
        options.Password.RequireDigit = PasswordPolicy.RequiresDigit;
        options.Password.RequireUppercase = PasswordPolicy.RequiresUppercase;
        options.Password.RequireLowercase = PasswordPolicy.RequiresLowercase;
        options.Password.RequireNonAlphanumeric = PasswordPolicy.RequiresNonAlphanumeric;
        options.Password.RequiredUniqueChars = 1;

        // FR-016. Both numbers are stated to the person, so they are
        // requirements rather than tuning.
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = AccountDefaults.MaxFailedAccessAttempts;
        options.Lockout.DefaultLockoutTimeSpan = AccountDefaults.LockoutDuration;
    })
    .AddEntityFrameworkStores<VoxLibDbContext>()
    .AddClaimsPrincipalFactory<ListenerClaimsFactory>()
    .AddDefaultTokenProviders()
    .AddSignInManager();

// FR-024 and the confirmation half of FR-008. Long enough that somebody who
// reads mail on another device and comes back after lunch still has a working
// link, and every flow offers a replacement anyway.
builder.Services.Configure<DataProtectionTokenProviderOptions>(
    options => options.TokenLifespan = AccountDefaults.LinkLifespan);

builder.Services
    .AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();

// The session lives on the server; the cookie carries only a key to it. FR-018
// requires that a session captured before signing out be refused afterwards, and
// also that signing out on one device leave another alone. A self-contained
// cookie cannot do the first at all, and the account's security stamp cannot do
// the second, because it belongs to an account rather than to a session.
builder.Services.AddSingleton<ITicketStore, DatabaseTicketStore>();

// Attached here rather than inside ConfigureApplicationCookie, because that
// callback has no service provider to resolve the store from.
builder.Services
    .AddOptions<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme)
    .Configure<ITicketStore>((options, store) => options.SessionStore = store);

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = AccountDefaults.SessionCookieName;

    // FR-013: a script injected into a page cannot read the session and so
    // cannot carry it elsewhere.
    options.Cookie.HttpOnly = true;

    // Strict rather than Lax, and it costs nothing here. Strict withholds the
    // cookie on a top-level navigation arriving from another site, which would
    // normally show a signed-in person as signed out. The account pages are not
    // generated ahead of time and establish who is signed in after the page
    // loads, and that request is same-site, so the cookie is sent.
    options.Cookie.SameSite = SameSiteMode.Strict;

    // The container's http profile has no https port, so requiring Secure there
    // would mean no session at all in development.
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;

    // FR-012 and SC-004: thirty days, extended by use. Long by general
    // standards and chosen deliberately, because retyping an address and a
    // password is an expensive interruption for someone navigating by screen
    // reader and a short session turns every visit into one.
    options.ExpireTimeSpan = AccountDefaults.SessionLifetime;
    options.SlidingExpiration = true;

    // This is a JSON API. The default handler redirects to a Razor page that
    // does not exist here, which would answer 302 to a fetch and look like
    // success to a caller that follows redirects.
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

// FR-014 and FR-022 both require a session to stop working before it expires,
// and SC-006 measures that on the session's next use. The default here is a
// thirty-minute timer, which would leave whoever took an account signed in for
// up to half an hour after its real owner recovered it. Zero means the check
// happens every request: one indexed read, paid deliberately.
//
// It does a second job for free. Revalidating rebuilds the principal from the
// store, so the beneficiary claim is refreshed on every request rather than
// frozen into a thirty-day cookie.
builder.Services.Configure<SecurityStampValidatorOptions>(
    options => options.ValidationInterval = TimeSpan.Zero);

// The key ring encrypts the session cookie and every confirmation and recovery
// link. Its default store is the file system under the user profile, which this
// container erases on rebuild and which two instances would not share, so losing
// it signs everyone out and voids every unfollowed link. The application name is
// pinned so purpose strings stay stable across restarts.
builder.Services
    .AddDataProtection()
    .PersistKeysToDbContext<VoxLibDbContext>()
    .SetApplicationName(AccountDefaults.ApplicationName);

// Registered now and attached to no endpoint, deliberately. This feature gates
// nothing; the point is that the audio feature adds one line to one endpoint
// rather than migrating how accounts are stored. FR-026.
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        AccountDefaults.SignedInPolicy,
        policy => policy.RequireAuthenticatedUser());

    options.AddPolicy(
        AccountDefaults.VerifiedBeneficiaryPolicy,
        policy => policy.RequireClaim(ListenerClaims.Beneficiary));
});

// The session cookie is attached by the browser automatically, so every request
// that changes state has to prove it came from this site. On .NET 10 the
// antiforgery middleware validates only endpoints that read form data, and every
// endpoint here binds JSON, so the token is checked explicitly in the handlers.
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = AccountDefaults.AntiforgeryHeaderName;
    options.Cookie.Name = AccountDefaults.AntiforgeryCookieName;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// The caller half of FR-017. The address half is keyed by the submitted address,
// which lives in the request body, so it is applied in the handlers instead:
// a middleware partitioner would have to read the body before the endpoint does.
// Configurable so the endpoint tests can raise it out of the way, and so the
// one test that is about the limit itself can lower it. A whole test run shares
// one client address, and a limit tuned for a real caller would refuse the
// tests rather than the attack.
var requestsPerClientWindow = builder.Configuration.GetValue(
    "RateLimit:RequestsPerClientWindow",
    AccountDefaults.RequestsPerClientWindow);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy(
        AccountDefaults.ByClientPolicy,
        context => RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = requestsPerClientWindow,
                Window = AccountDefaults.ClientWindow,
                QueueLimit = 0,
            }));

    options.OnRejected = async (context, cancellationToken) =>
    {
        // The same shape a lockout produces, headers included, so that being
        // refused for trying too often never reveals which of the two happened.
        context.HttpContext.Response.Headers.RetryAfter =
            ((int)AccountDefaults.ClientWindow.TotalSeconds).ToString();

        await context.HttpContext.Response.WriteAsJsonAsync(
            AccountProblems.TooManyAttempts(AccountDefaults.ClientWindow),
            cancellationToken);
    };
});

builder.Services.AddScoped<ISessionStore, SessionStore>();
builder.Services.AddScoped<IAccountStore, AccountStore>();
builder.Services.AddScoped<IAccountLinks, AccountLinks>();
builder.Services.AddScoped<IAccountSession, HttpAccountSession>();
builder.Services.AddScoped<IAccountMessages, AccountMessages>();
builder.Services.AddSingleton<IAttemptLimiter, AttemptLimiter>();
builder.Services.AddScoped<Registration>();
builder.Services.AddScoped<EmailConfirmation>();
builder.Services.AddScoped<SignIn>();
builder.Services.AddScoped<PasswordRecovery>();

builder.Services.AddSingleton(
    new SiteAddress(
        builder.Configuration["App:BaseAddress"] is { Length: > 0 } baseAddress
            ? baseAddress
            : "http://localhost:5173"));

builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection(SmtpOptions.Section));
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();

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

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// FR-028 and Principle IV: robots.txt is advisory and is never the protection.
// Every response about an account says so, whatever it is.
//
// Set when the response starts rather than now, because a request that ends in
// an exception is answered by the handler above, and that clears the response
// first. Setting the header here would lose it on exactly the responses nobody
// checks.
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/account"))
    {
        context.Response.OnStarting(state =>
        {
            ((HttpContext)state).Response.Headers["X-Robots-Tag"] = "noindex";
            return Task.CompletedTask;
        }, context);
    }

    await next();
});

app.MapGet("/health", () => Results.Ok(new { status = "ok", utc = DateTimeOffset.UtcNow }))
   .WithName("Health");

app.MapBookEndpoints();
app.MapAccountEndpoints();
app.MapSessionEndpoints();

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
