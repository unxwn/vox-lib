using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using VoxLib.Model.Account;

namespace VoxLib.Dal.Account;

/// <summary>
/// Puts beneficiary status on the principal, so an access decision reads a claim
/// rather than the database. FR-026.
/// <para>
/// The claim is not stale, because the session is revalidated against the store
/// on every request and that rebuilds the principal through this factory.
/// Beneficiary status can be withdrawn, and a claim frozen into a thirty-day
/// cookie could not honour that.
/// </para>
/// </summary>
public sealed class ListenerClaimsFactory(
    UserManager<AccountDao> users,
    IOptions<IdentityOptions> options) : UserClaimsPrincipalFactory<AccountDao>(users, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AccountDao user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        if (user.IsVerifiedBeneficiary)
        {
            // Present or absent, never "false". A policy asks whether the claim
            // is there, so an account that loses the status loses the claim
            // rather than carrying one that says no.
            identity.AddClaim(new Claim(ListenerClaims.Beneficiary, "true"));
        }

        if (user.Email is { Length: > 0 } email
            && identity.FindFirst(ClaimTypes.Email) is null)
        {
            identity.AddClaim(new Claim(ClaimTypes.Email, email));
        }

        return identity;
    }
}
