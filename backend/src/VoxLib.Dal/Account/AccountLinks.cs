using Microsoft.AspNetCore.Identity;
using VoxLib.Model.Account;

namespace VoxLib.Dal.Account;

/// <summary>
/// Single-use, time-limited links over the identity component's token
/// providers. The token carries the account's security stamp, which is what
/// makes it single-use without a table of issued links to keep: setting a
/// password rotates the stamp and every outstanding token stops working.
/// <para>
/// Confirmation is the exception, because confirming an address does not rotate
/// the stamp. That is handled where it belongs, in the order of checks the
/// orchestrator makes, so that a second visit says the address is already
/// confirmed rather than reporting an error. FR-008.
/// </para>
/// </summary>
public sealed class AccountLinks(UserManager<AccountDao> users) : IAccountLinks
{
    public async Task<string> IssueAsync(
        Guid accountId,
        LinkPurpose purpose,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var account = await users.FindByIdAsync(accountId.ToString())
            ?? throw new InvalidOperationException($"No account with id {accountId}.");

        return purpose switch
        {
            LinkPurpose.ConfirmEmail => await users.GenerateEmailConfirmationTokenAsync(account),
            LinkPurpose.ResetPassword => await users.GeneratePasswordResetTokenAsync(account),
            _ => throw new ArgumentOutOfRangeException(nameof(purpose)),
        };
    }

    public async Task<bool> IsValidAsync(
        Guid accountId,
        LinkPurpose purpose,
        string token,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (await users.FindByIdAsync(accountId.ToString()) is not { } account)
        {
            return false;
        }

        // The purpose is part of what is verified, so a confirmation token
        // cannot be presented as a recovery token or the other way round.
        var tokenPurpose = purpose switch
        {
            LinkPurpose.ConfirmEmail => UserManager<AccountDao>.ConfirmEmailTokenPurpose,
            LinkPurpose.ResetPassword => UserManager<AccountDao>.ResetPasswordTokenPurpose,
            _ => throw new ArgumentOutOfRangeException(nameof(purpose)),
        };

        return await users.VerifyUserTokenAsync(
            account,
            users.Options.Tokens.EmailConfirmationTokenProvider,
            tokenPurpose,
            token);
    }
}
