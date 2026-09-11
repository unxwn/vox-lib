using VoxLib.Model.Account;

namespace VoxLib.Orchestrator.Account;

/// <summary>
/// Deciding whether somebody may sign in, and which of the four answers applies.
/// <para>
/// The order of what happens here is the requirement, not an implementation
/// detail. An unknown address still spends the cost of verifying a password, so
/// that the time taken does not tell an attacker which addresses exist. The
/// address being unconfirmed is only reported once the supplied password has
/// verified, so that the one response admitting an account exists is reachable
/// only by somebody who already knew its password. Rearranging these lines
/// breaks FR-005 without breaking a single test that is about signing in.
/// </para>
/// </summary>
public sealed class SignIn(
    IAccountStore accounts,
    IAccountSession session,
    IAttemptLimiter limiter)
{
    public async Task<SignInResult> AttemptAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        // Checked before anything else, and keyed by the address rather than by
        // the caller. An address with no account runs out of attempts at the
        // same point a real one is locked out, which is what lets a lockout be
        // explained to a person without telling a stranger that the address is
        // registered. Checking rather than consuming, because only a failure
        // costs an allowance: that is how a lockout counter behaves, and the two
        // have to behave alike.
        var allowance = limiter.Check(email, AttemptKind.SignIn);

        if (!allowance.Allowed)
        {
            return SignInResult.LockedOut(allowance.RetryAfter);
        }

        var account = await accounts.FindAsync(email, cancellationToken);

        if (account is null)
        {
            // Spend the work anyway. Returning here without hashing is what
            // makes an unknown address measurably faster than a wrong password,
            // and SC-007 is about exactly that difference.
            await accounts.BurnPasswordVerificationAsync(password, cancellationToken);

            var spent = limiter.Consume(email, AttemptKind.SignIn);

            // The attempt that exhausts the allowance is answered the way the
            // attempt that applies a lockout is, so the change of answer happens
            // at the same point for an address that exists and one that does not.
            return spent.Allowed
                ? SignInResult.NotRecognised()
                : SignInResult.LockedOut(spent.RetryAfter);
        }

        if (account.LockedOutUntil is { } until && until > DateTimeOffset.UtcNow)
        {
            return SignInResult.LockedOut(until - DateTimeOffset.UtcNow);
        }

        if (!await accounts.VerifyPasswordAsync(account.Id, password, cancellationToken))
        {
            // Counted here too, so the two counters stay in step. The identity
            // component's is authoritative for a real account; this one is what
            // an address with no account is measured against, and they only look
            // alike from outside if both move on the same events.
            limiter.Consume(email, AttemptKind.SignIn);

            // Re-read, because the failed attempt may have been the one that
            // applied the lockout, and a person who has just locked themselves
            // out is owed the explanation rather than another identical
            // refusal. FR-016.
            var after = await accounts.FindAsync(email, cancellationToken);

            return after?.LockedOutUntil is { } lockedUntil && lockedUntil > DateTimeOffset.UtcNow
                ? SignInResult.LockedOut(lockedUntil - DateTimeOffset.UtcNow)
                : SignInResult.NotRecognised();
        }

        // Only now, with the correct password already supplied, may the product
        // admit that this address has an account. FR-011 inside FR-005.
        if (!account.EmailConfirmed)
        {
            return SignInResult.EmailNotConfirmed();
        }

        // A successful sign-in clears the count, exactly as it clears the
        // identity component's failure count.
        limiter.Clear(email, AttemptKind.SignIn);

        await session.StartAsync(account.Id, cancellationToken);

        return SignInResult.Succeeded(account.ToListener());
    }

    public async Task SignOutAsync(CancellationToken cancellationToken) =>
        await session.EndAsync(cancellationToken);

    public Listener Current => session.Current;
}
