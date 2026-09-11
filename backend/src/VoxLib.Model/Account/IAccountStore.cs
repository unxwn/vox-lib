namespace VoxLib.Model.Account;

/// <summary>
/// The credential record as the rules see it. Implemented in VoxLib.Dal over the
/// platform's identity component, so that hashing, lockout counting and the
/// stamp that ends sessions are not written by hand.
/// <para>
/// Nothing about how a credential is stored crosses this interface: no hash, no
/// stamp, no failed count. What comes back is a <see cref="Listener"/>.
/// </para>
/// </summary>
public interface IAccountStore
{
    /// <summary>The account for this address, or null. Matched without regard to letter case.</summary>
    Task<AccountSnapshot?> FindAsync(string email, CancellationToken cancellationToken);

    /// <summary>
    /// The account with this id, or null. Used where the id came from a link
    /// rather than from a person, so "no such account" and "bad token" have to
    /// reach the caller as the same answer.
    /// </summary>
    Task<AccountSnapshot?> FindByIdAsync(Guid accountId, CancellationToken cancellationToken);

    /// <summary>
    /// Creates an account with the address unconfirmed. Returns null when the
    /// address already has one, including when two identical registrations race:
    /// the unique index on the address is what settles it, so exactly one
    /// account exists afterwards. FR-006.
    /// </summary>
    Task<AccountSnapshot?> CreateAsync(
        string email,
        string password,
        CancellationToken cancellationToken);

    /// <summary>
    /// Whether this is the account's password. Records a failure and applies the
    /// lockout when it is not, and clears the failure count when it is.
    /// </summary>
    Task<bool> VerifyPasswordAsync(
        Guid accountId,
        string password,
        CancellationToken cancellationToken);

    /// <summary>
    /// Spends the same work verifying a password as
    /// <see cref="VerifyPasswordAsync"/> does, against a fixed hash belonging to
    /// nobody. Called when the address is unknown so that the time taken cannot
    /// tell an unknown address from a wrong password. FR-005, and SC-007
    /// measures it.
    /// </summary>
    Task BurnPasswordVerificationAsync(string password, CancellationToken cancellationToken);

    /// <summary>Records the address as confirmed. Idempotent.</summary>
    Task ConfirmEmailAsync(Guid accountId, CancellationToken cancellationToken);

    /// <summary>
    /// Redeems a recovery token and sets a new password, then clears any lockout
    /// and records the address as confirmed. FR-021, FR-022 and FR-023 are one
    /// operation because they are one moment.
    /// <para>
    /// The token is redeemed here rather than checked separately first, so that
    /// validating it and replacing the password cannot come apart and leave an
    /// account with no password at all.
    /// </para>
    /// </summary>
    Task<RecoveryOutcome> SetPasswordAsync(
        Guid accountId,
        string token,
        string password,
        CancellationToken cancellationToken);

    /// <summary>
    /// Ends every session for this account, taking effect on each one's next
    /// use. FR-014. Recovery does this on its own; this is the way to do it
    /// without setting a password.
    /// </summary>
    Task EndAllSessionsAsync(Guid accountId, CancellationToken cancellationToken);
}

/// <summary>
/// What the rules need to know about an account, read at one moment. Separate
/// from <see cref="Listener"/> because the rules need the confirmation state and
/// the lockout, and an access decision must not see either.
/// </summary>
public sealed record AccountSnapshot(
    Guid Id,
    string Email,
    bool EmailConfirmed,
    bool IsVerifiedBeneficiary,
    DateTimeOffset? LockedOutUntil)
{
    public Listener ToListener() => Listener.SignedIn(Id, Email, IsVerifiedBeneficiary);
}
