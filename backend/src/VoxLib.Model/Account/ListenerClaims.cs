namespace VoxLib.Model.Account;

/// <summary>
/// The claim types an access decision is made from. Named here so the side that
/// writes them and the side that reads them cannot disagree.
/// </summary>
public static class ListenerClaims
{
    /// <summary>
    /// Present only when the account is verified as entitled to
    /// accessible-format copies. A claim rather than a role: there is exactly
    /// one distinction today, and a group mechanism for a single distinction is
    /// ceremony. FR-026.
    /// </summary>
    public const string Beneficiary = "voxlib:beneficiary";
}
