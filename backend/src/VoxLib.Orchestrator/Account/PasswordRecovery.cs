using Microsoft.Extensions.Logging;
using VoxLib.Model.Account;

namespace VoxLib.Orchestrator.Account;

/// <summary>
/// Getting back into an account nobody can otherwise reach.
/// <para>
/// This is where FR-021, FR-022 and FR-023 live together, because they are one
/// moment rather than three features. Setting a password through recovery clears
/// the lockout that probably sent the person here, ends every session that
/// existed beforehand so that recovering an account somebody else is holding
/// evicts them, and records the address as confirmed because following a link
/// sent there proves exactly what confirmation proves.
/// </para>
/// </summary>
public sealed class PasswordRecovery(
    IAccountStore accounts,
    IAccountLinks links,
    IAccountMessages messages,
    IAttemptLimiter limiter,
    ILogger<PasswordRecovery> logger)
{
    /// <summary>
    /// Sends a recovery link, if the address has an account and the limit allows
    /// it.
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

        if (account is null)
        {
            return;
        }

        var token = await links.IssueAsync(
            account.Id,
            LinkPurpose.ResetPassword,
            cancellationToken);

        await MessageDelivery.TryAsync(
            () => messages.SendRecoveryAsync(account.Id, email, token, cancellationToken),
            logger,
            "recovery");
    }

    public async Task<RecoveryOutcome> SetPasswordAsync(
        Guid accountId,
        string token,
        string password,
        CancellationToken cancellationToken)
    {
        // Checked before the token is spent. A password that does not meet the
        // policy is a mistake worth retyping, and it must not cost the person
        // the only link they have back into their account.
        if (!PasswordPolicy.IsLongEnough(password))
        {
            return RecoveryOutcome.PasswordRejected;
        }

        // The store redeems the token and replaces the hash in one step, and
        // replacing the hash rotates the security stamp. That is what ends every
        // session that existed beforehand and makes this same link unusable
        // afterwards: FR-022 and FR-024 out of one operation, with no table of
        // issued links to keep.
        var outcome = await accounts.SetPasswordAsync(
            accountId,
            token,
            password,
            cancellationToken);

        if (outcome != RecoveryOutcome.PasswordSet)
        {
            return outcome;
        }

        // FR-021 is only half done by the store. Two things refuse a sign-in
        // after repeated failures: the account's own lockout, which the store
        // clears, and the attempts counted against the address, which it knows
        // nothing about. Leaving the second in place means a person recovers
        // their account and is still refused for another quarter of an hour,
        // which is the exact experience this requirement exists to prevent.
        if (await accounts.FindByIdAsync(accountId, cancellationToken) is { } account)
        {
            limiter.Clear(account.Email, AttemptKind.SignIn);
        }

        return outcome;
    }
}
