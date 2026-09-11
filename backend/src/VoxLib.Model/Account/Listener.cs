namespace VoxLib.Model.Account;

/// <summary>
/// The identity that access decisions are made about. Deliberately not the
/// credential record: it carries nothing about how a password is stored, so how
/// credentials are kept can change without touching the rules that use it.
/// </summary>
public sealed class Listener
{
    /// <summary>
    /// Nobody is signed in. A value rather than a null, so every caller handles
    /// the case instead of forgetting it.
    /// </summary>
    public static readonly Listener Anonymous = new();

    private Listener()
    {
    }

    private Listener(Guid accountId, string email, bool isVerifiedBeneficiary)
    {
        AccountId = accountId;
        Email = email;
        IsVerifiedBeneficiary = isVerifiedBeneficiary;
    }

    /// <summary>Null for <see cref="Anonymous"/>.</summary>
    public Guid? AccountId { get; }

    /// <summary>Null for <see cref="Anonymous"/>.</summary>
    public string? Email { get; }

    /// <summary>
    /// Whether this person has been verified as entitled to accessible-format
    /// copies. Always false for now: there is no organisation to verify against
    /// yet, so the field exists so that adding the rule later is a change to one
    /// access rule rather than a migration. FR-025.
    /// </summary>
    public bool IsVerifiedBeneficiary { get; }

    public bool IsSignedIn => AccountId is not null;

    public static Listener SignedIn(Guid accountId, string email, bool isVerifiedBeneficiary) =>
        new(accountId, email, isVerifiedBeneficiary);
}
