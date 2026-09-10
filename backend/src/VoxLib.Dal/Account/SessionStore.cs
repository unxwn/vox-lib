using Microsoft.EntityFrameworkCore;
using VoxLib.Dal.Persistence;
using VoxLib.Model.Account;

namespace VoxLib.Dal.Account;

/// <summary>
/// Sessions in the database rather than in memory.
/// <para>
/// In memory would mean every restart signs everyone out, and two instances
/// could not read each other's sessions. That is the same reasoning that puts
/// the data protection keys here, and it matters more for sessions: this
/// product's session lasts thirty days precisely because retyping a password is
/// an expensive interruption for somebody navigating by screen reader.
/// </para>
/// </summary>
public sealed class SessionStore(VoxLibDbContext database, TimeProvider clock) : ISessionStore
{
    public async Task<string> CreateAsync(
        byte[] payload,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        // Opportunistic rather than scheduled. A background sweep is the right
        // answer once there is enough traffic to need one; until then this keeps
        // the table from growing without adding a service to run and watch.
        await RemoveExpiredAsync(cancellationToken);

        var session = new SessionDao
        {
            Id = Guid.CreateVersion7().ToString("N"),
            Payload = payload,
            ExpiresAt = expiresAt,
        };

        database.Sessions.Add(session);
        await database.SaveChangesAsync(cancellationToken);

        return session.Id;
    }

    public async Task<byte[]?> FindAsync(string key, CancellationToken cancellationToken)
    {
        var session = await database.Sessions
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == key, cancellationToken);

        // An expired row is treated as absent rather than deleted here, so that
        // reading a session never turns into a write on the hot path.
        return session is null || session.ExpiresAt <= clock.GetUtcNow()
            ? null
            : session.Payload;
    }

    public async Task RenewAsync(
        string key,
        byte[] payload,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        var session = await database.Sessions
            .SingleOrDefaultAsync(candidate => candidate.Id == key, cancellationToken);

        if (session is null)
        {
            // Renewing a session that has been ended must not bring it back.
            return;
        }

        session.Payload = payload;
        session.ExpiresAt = expiresAt;

        await database.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken)
    {
        await database.Sessions
            .Where(session => session.Id == key)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private Task RemoveExpiredAsync(CancellationToken cancellationToken) =>
        database.Sessions
            .Where(session => session.ExpiresAt <= clock.GetUtcNow())
            .ExecuteDeleteAsync(cancellationToken);
}
