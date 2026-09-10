using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using VoxLib.Model.Account;

namespace VoxLib.Api.Account;

/// <summary>
/// Keeps the session on the server and leaves only a key in the cookie.
/// <para>
/// This exists for one requirement. FR-018 says a session captured before
/// signing out must be refused afterwards, and its acceptance scenarios also say
/// that signing out on one device leaves another signed in. Nothing about a
/// self-contained cookie can do the first, because the cookie stays valid until
/// it expires however many times its owner signs out. Rotating the account's
/// security stamp does the first and breaks the second, because a stamp belongs
/// to an account rather than to a session. A session that lives on the server
/// does both, and deleting one row is what ends one session.
/// </para>
/// <para>
/// It costs a read per authenticated request. The session is already revalidated
/// against the store on every request for FR-014, so this joins work that was
/// happening anyway rather than adding a round trip of its own.
/// </para>
/// <para>
/// It lives in VoxLib.Api for the same reason the session itself does: what a
/// ticket contains is a matter for the web framework that issues it. The storage
/// behind it is an interface in VoxLib.Model.
/// </para>
/// </summary>
public sealed class DatabaseTicketStore(
    IServiceScopeFactory scopes,
    IDataProtectionProvider protection) : ITicketStore
{
    private const string Purpose = "VoxLib.Session";

    private readonly TicketDataFormat _format = new(protection.CreateProtector(Purpose));

    public async Task<string> StoreAsync(AuthenticationTicket ticket) =>
        await WithStoreAsync(store =>
            store.CreateAsync(Serialize(ticket), Expiry(ticket), CancellationToken.None));

    public async Task RenewAsync(string key, AuthenticationTicket ticket) =>
        await WithStoreAsync(store =>
            store.RenewAsync(key, Serialize(ticket), Expiry(ticket), CancellationToken.None));

    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        var payload = await WithStoreAsync(store => store.FindAsync(key, CancellationToken.None));

        return payload is null ? null : _format.Unprotect(Encoding.UTF8.GetString(payload));
    }

    /// <summary>
    /// Ends one session. This is the line that makes a captured cookie useless
    /// the moment its owner signs out.
    /// </summary>
    public async Task RemoveAsync(string key) =>
        await WithStoreAsync(store => store.RemoveAsync(key, CancellationToken.None));

    private byte[] Serialize(AuthenticationTicket ticket) =>
        Encoding.UTF8.GetBytes(_format.Protect(ticket));

    private static DateTimeOffset Expiry(AuthenticationTicket ticket) =>
        ticket.Properties.ExpiresUtc
            ?? DateTimeOffset.UtcNow.Add(AccountDefaults.SessionLifetime);

    /// <summary>
    /// A scope per operation. The framework resolves a ticket store once, for
    /// the lifetime of the application, while the storage behind it reaches the
    /// database and therefore belongs to a request.
    /// </summary>
    private async Task<T> WithStoreAsync<T>(Func<ISessionStore, Task<T>> work)
    {
        await using var scope = scopes.CreateAsyncScope();

        return await work(scope.ServiceProvider.GetRequiredService<ISessionStore>());
    }

    private async Task WithStoreAsync(Func<ISessionStore, Task> work)
    {
        await using var scope = scopes.CreateAsyncScope();

        await work(scope.ServiceProvider.GetRequiredService<ISessionStore>());
    }
}
