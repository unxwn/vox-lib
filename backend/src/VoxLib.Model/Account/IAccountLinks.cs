namespace VoxLib.Model.Account;

/// <summary>
/// The single-use, time-limited proof that a person controls an address. One
/// mechanism, two purposes, which is why recovery costs so little on top of
/// confirmation.
/// <para>
/// A token carries the account's security stamp, so setting a password makes
/// every outstanding token for that account unredeemable without a table of
/// issued links to maintain.
/// </para>
/// </summary>
public interface IAccountLinks
{
    Task<string> IssueAsync(Guid accountId, LinkPurpose purpose, CancellationToken cancellationToken);

    /// <summary>
    /// Whether this token is still valid for this account and this purpose.
    /// False for a token that has been used, has expired, or was issued for the
    /// other purpose.
    /// </summary>
    Task<bool> IsValidAsync(
        Guid accountId,
        LinkPurpose purpose,
        string token,
        CancellationToken cancellationToken);
}
