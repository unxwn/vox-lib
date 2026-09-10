namespace VoxLib.Model.Account;

/// <summary>Why a sign-in attempt ended the way it did.</summary>
public enum SignInOutcome
{
    Succeeded,

    /// <summary>
    /// The combination was not recognised. One value for an unknown address and
    /// for a known address with the wrong password, because FR-015 requires the
    /// same answer to both and a second value here would be one an endpoint
    /// could accidentally reveal.
    /// </summary>
    NotRecognised,

    /// <summary>
    /// The password was correct but the address has not been confirmed. This is
    /// the single case where the product admits an account exists, and it is
    /// reachable only by supplying that account's password. FR-011 inside FR-005.
    /// </summary>
    EmailNotConfirmed,

    /// <summary>
    /// Too many consecutive failures. <see cref="SignInResult.RetryAfter"/>
    /// carries when the person may try again, which FR-016 requires them to be
    /// told.
    /// </summary>
    LockedOut,
}

/// <summary>
/// The outcome of a sign-in attempt, with the listener when it succeeded and the
/// wait when it was refused for trying too often.
/// </summary>
public sealed record SignInResult(
    SignInOutcome Outcome,
    Listener Listener,
    TimeSpan? RetryAfter = null)
{
    public static SignInResult Succeeded(Listener listener) =>
        new(SignInOutcome.Succeeded, listener);

    public static SignInResult NotRecognised() =>
        new(SignInOutcome.NotRecognised, Listener.Anonymous);

    public static SignInResult EmailNotConfirmed() =>
        new(SignInOutcome.EmailNotConfirmed, Listener.Anonymous);

    public static SignInResult LockedOut(TimeSpan retryAfter) =>
        new(SignInOutcome.LockedOut, Listener.Anonymous, retryAfter);
}
