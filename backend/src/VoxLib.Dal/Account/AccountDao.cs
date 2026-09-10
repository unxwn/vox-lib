using Microsoft.AspNetCore.Identity;

namespace VoxLib.Dal.Account;

/// <summary>
/// The credential record. Most of it comes from <see cref="IdentityUser{TKey}"/>:
/// the address and its normalized form, whether it is confirmed, the password
/// hash, the security stamp that ends sessions, the consecutive failure count
/// and the lockout.
/// <para>
/// The base type also brings phone number and two-factor columns that this
/// product does not use. They are the price of using the ready-made Entity
/// Framework store rather than writing one, which would be a large amount of
/// code to avoid five columns that are always empty.
/// </para>
/// </summary>
public sealed class AccountDao : IdentityUser<Guid>
{
    /// <summary>
    /// Whether this person has been verified as entitled to accessible-format
    /// copies. Nothing in this feature ever sets it true: there is no
    /// organisation to verify against yet. It exists now so that the audio
    /// feature is a change to one access rule rather than a migration and an
    /// audit of every call site. FR-025.
    /// </summary>
    public bool IsVerifiedBeneficiary { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
