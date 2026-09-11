using Microsoft.Extensions.Logging;
using VoxLib.Model.Account;

namespace VoxLib.Orchestrator.Account;

/// <summary>
/// Confirming an address, and sending another message when the first one did not
/// arrive.
/// <para>
/// The decision this class holds is the order of the checks. Whether the address
/// is already confirmed is asked before the token is validated, and that order
/// is the whole of FR-008: confirming does not rotate the security stamp the
/// token carries, so the token stays technically valid afterwards and the
/// distinction between "already done" and "no longer works" has to come from
/// somewhere. Reversing these two lines turns an ordinary second click into an
/// error message.
/// </para>
/// </summary>
public sealed class EmailConfirmation(
    IAccountStore accounts,
    IAccountLinks links,
    IAccountMessages messages,
    IAttemptLimiter limiter,
    ILogger<EmailConfirmation> logger)
{
    public async Task<ConfirmationOutcome> ConfirmAsync(
        Guid accountId,
        string token,
        CancellationToken cancellationToken)
    {
        var account = await accounts.FindByIdAsync(accountId, cancellationToken);

        if (account is null)
        {
            // Indistinguishable from a bad token. An account id taken from a
            // link must not reveal whether it names anything.
            return ConfirmationOutcome.LinkExpired;
        }

        if (account.EmailConfirmed)
        {
            return ConfirmationOutcome.AlreadyConfirmed;
        }

        if (!await links.IsValidAsync(accountId, LinkPurpose.ConfirmEmail, token, cancellationToken))
        {
            return ConfirmationOutcome.LinkExpired;
        }

        await accounts.ConfirmEmailAsync(accountId, cancellationToken);

        return ConfirmationOutcome.Confirmed;
    }

    /// <summary>
    /// Sends another confirmation message, if there is an unconfirmed account
    /// for the address and the limit allows it.
    /// <para>
    /// It returns nothing, deliberately. The caller answers the same either way,
    /// because whether a message went out is exactly the fact FR-005 forbids
    /// disclosing.
    /// </para>
    /// </summary>
    public async Task RequestAsync(string email, CancellationToken cancellationToken)
    {
        if (!limiter.Consume(email, AttemptKind.SendMessage).Allowed)
        {
            return;
        }

        var account = await accounts.FindAsync(email, cancellationToken);

        if (account is null || account.EmailConfirmed)
        {
            return;
        }

        var token = await links.IssueAsync(
            account.Id,
            LinkPurpose.ConfirmEmail,
            cancellationToken);

        await MessageDelivery.TryAsync(
            () => messages.SendConfirmationAsync(account.Id, email, token, cancellationToken),
            logger,
            "confirmation");
    }
}
