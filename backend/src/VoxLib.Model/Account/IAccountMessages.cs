namespace VoxLib.Model.Account;

/// <summary>
/// The messages this feature sends, in Ukrainian. Delivery is somebody else's
/// job; this decides what is said and to whom.
/// </summary>
public interface IAccountMessages
{
    /// <summary>Carries the link that confirms the address. FR-004.</summary>
    Task SendConfirmationAsync(
        Guid accountId,
        string email,
        string token,
        CancellationToken cancellationToken);

    /// <summary>
    /// Sent instead of a confirmation when the address already has an account.
    /// This is what lets registration answer identically in both cases while
    /// still telling the person who owns the address something useful. FR-005.
    /// </summary>
    Task SendAlreadyRegisteredAsync(string email, CancellationToken cancellationToken);

    /// <summary>Carries the link that allows a new password to be set. FR-020.</summary>
    Task SendRecoveryAsync(
        Guid accountId,
        string email,
        string token,
        CancellationToken cancellationToken);
}
