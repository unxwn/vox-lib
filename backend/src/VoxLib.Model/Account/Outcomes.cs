namespace VoxLib.Model.Account;

/// <summary>
/// What registering did.
/// <para>
/// There is deliberately no "already exists" value. The orchestrator knows, and
/// the caller never does: FR-005 forbids disclosing through any response whether
/// an address has an account, so the difference goes to the address itself as a
/// message rather than into the answer.
/// </para>
/// </summary>
public enum RegistrationOutcome
{
    /// <summary>
    /// The submission was accepted and a message has been sent. Returned whether
    /// the address was new or already had an account.
    /// </summary>
    Accepted,

    /// <summary>The password does not meet <see cref="PasswordPolicy"/>.</summary>
    PasswordRejected,
}

/// <summary>What confirming an address did. FR-008 needs all three told apart.</summary>
public enum ConfirmationOutcome
{
    Confirmed,

    /// <summary>
    /// Not an error. Following the same link twice is ordinary, and reporting it
    /// as a failure sends a person looking for a problem that is not there.
    /// </summary>
    AlreadyConfirmed,

    LinkExpired,
}

/// <summary>What setting a password through recovery did.</summary>
public enum RecoveryOutcome
{
    PasswordSet,

    /// <summary>The link has been used or has passed its lifetime.</summary>
    LinkExpired,

    PasswordRejected,
}

/// <summary>
/// Which of the two things a single-use link proves. Both are the same
/// mechanism, a time-limited token sent to an address, used for two purposes.
/// </summary>
public enum LinkPurpose
{
    ConfirmEmail,
    ResetPassword,
}
