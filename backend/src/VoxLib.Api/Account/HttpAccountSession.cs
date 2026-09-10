using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using VoxLib.Dal.Account;
using VoxLib.Model.Account;

namespace VoxLib.Api.Account;

/// <summary>
/// This browser's session, written as a cookie on the request being handled.
/// <para>
/// It lives in VoxLib.Api rather than beside the rest of the account
/// infrastructure because issuing a cookie needs the request it belongs to, and
/// the type that does it is part of the web framework. Putting it anywhere else
/// would mean giving a class library a framework reference in order to carry a
/// transport concern. The interface stays in VoxLib.Model, so the rules name the
/// intent and never the mechanism.
/// </para>
/// </summary>
public sealed class HttpAccountSession(
    SignInManager<AccountDao> signIn,
    IHttpContextAccessor context) : IAccountSession
{
    public async Task StartAsync(Guid accountId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var account = await signIn.UserManager.FindByIdAsync(accountId.ToString())
            ?? throw new InvalidOperationException($"No account with id {accountId}.");

        // Persistent, so closing the browser does not sign the person out. For
        // someone navigating by screen reader, retyping an address and a
        // password is an expensive interruption, and a session that does not
        // survive a restart turns every visit into one. FR-012.
        await signIn.SignInAsync(account, isPersistent: true);
    }

    public async Task EndAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await signIn.SignOutAsync();
    }

    public Listener Current
    {
        get
        {
            var user = context.HttpContext?.User;

            if (user?.Identity?.IsAuthenticated != true)
            {
                return Listener.Anonymous;
            }

            var id = user.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(id, out var accountId))
            {
                return Listener.Anonymous;
            }

            // The user name is the address, so either claim carries it. Reading
            // both means a principal built before the email claim was added
            // still answers correctly.
            var email = user.FindFirstValue(ClaimTypes.Email)
                ?? user.FindFirstValue(ClaimTypes.Name)
                ?? string.Empty;

            return Listener.SignedIn(
                accountId,
                email,
                user.HasClaim(claim => claim.Type == ListenerClaims.Beneficiary));
        }
    }
}
