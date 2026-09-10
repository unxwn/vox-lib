namespace VoxLib.Model.Account;

/// <summary>
/// How often something may be attempted against one email address.
/// <para>
/// FR-017 asks for this so that per-account lockout is not the only defence
/// against attempts spread across many addresses, and FR-009 asks for it so the
/// resend flow cannot be used to send mail repeatedly to somebody who did not
/// ask for it.
/// </para>
/// <para>
/// Its third job is the one worth reading twice. FR-016 requires telling a
/// person that they are locked out and when they may return; FR-005 forbids
/// revealing whether an address has an account. A lockout message satisfies the
/// first and breaks the second, unless an address with no account is refused in
/// exactly the same way at exactly the same point. That is what this provides,
/// which is why the sign-in thresholds here must stay equal to the lockout
/// thresholds the identity component is configured with, and why a failed
/// attempt consumes an allowance while a successful one clears it: those are the
/// semantics a lockout counter has, so anything else would drift apart from it.
/// </para>
/// </summary>
public interface IAttemptLimiter
{
    /// <summary>Whether another attempt is allowed, without consuming one.</summary>
    AttemptDecision Check(string address, AttemptKind kind);

    /// <summary>Records one attempt and says whether it was within the allowance.</summary>
    AttemptDecision Consume(string address, AttemptKind kind);

    /// <summary>
    /// Forgets the attempts recorded against this address, as a successful
    /// sign-in does to a lockout counter.
    /// </summary>
    void Clear(string address, AttemptKind kind);
}

public enum AttemptKind
{
    /// <summary>
    /// Failed sign-in attempts. Counted with the same threshold and window the
    /// account lockout uses, so the two are indistinguishable from outside.
    /// </summary>
    SignIn,

    /// <summary>
    /// Messages sent to an address. Every request consumes one, because the harm
    /// being limited is the message arriving, not the request failing.
    /// </summary>
    SendMessage,
}

public sealed record AttemptDecision(bool Allowed, TimeSpan RetryAfter)
{
    public static readonly AttemptDecision Allow = new(true, TimeSpan.Zero);

    public static AttemptDecision Refuse(TimeSpan retryAfter) => new(false, retryAfter);
}
