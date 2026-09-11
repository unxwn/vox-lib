namespace VoxLib.Model.Account;

/// <summary>
/// Where a browser's session actually lives.
/// <para>
/// The cookie carries only a key; everything about the session is held here.
/// That is what FR-018 needs and what nothing else provides: signing out has to
/// refuse a session that was captured beforehand, and it has to leave the same
/// account's other devices alone. Rotating the account's security stamp does the
/// first and breaks the second, because a stamp belongs to an account rather
/// than to a session. A self-contained cookie does neither.
/// </para>
/// <para>
/// The payload is opaque here on purpose. What a session contains is a matter
/// for the transport that issues it; what it means to end one is not.
/// </para>
/// </summary>
public interface ISessionStore
{
    Task<string> CreateAsync(
        byte[] payload,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken);

    Task<byte[]?> FindAsync(string key, CancellationToken cancellationToken);

    Task RenewAsync(
        string key,
        byte[] payload,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken);

    /// <summary>Ends one session. The account's other sessions are untouched.</summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken);
}
