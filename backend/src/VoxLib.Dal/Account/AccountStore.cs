using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VoxLib.Model.Account;

namespace VoxLib.Dal.Account;

/// <summary>
/// The credential record over the platform's identity component. Password
/// hashing, the consecutive failure count and the lockout are the three things
/// in this feature where a mistake is invisible until it matters, so none of
/// them is written by hand here.
/// <para>
/// This deliberately uses <see cref="UserManager{TUser}"/> and not
/// <c>SignInManager</c>. SignInManager lives in the ASP.NET Core shared
/// framework and needs a request; keeping it out means this project stays a
/// plain class library and the transport stays at the edge.
/// </para>
/// </summary>
public sealed class AccountStore(UserManager<AccountDao> users, TimeProvider clock) : IAccountStore
{
    /// <summary>
    /// A hash of a password nobody has. Verifying against it costs what
    /// verifying a real one costs, which is how an unknown address is made to
    /// take the same time as a wrong password. FR-005, measured by SC-007.
    /// </summary>
    private static readonly Lazy<string> DummyHash = new(() =>
        new PasswordHasher<AccountDao>().HashPassword(
            new AccountDao(),
            "there is no account with this address"));

    private static readonly PasswordHasher<AccountDao> Hasher = new();

    public async Task<AccountSnapshot?> FindAsync(string email, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var account = await users.FindByEmailAsync(email);

        return account is null ? null : ToSnapshot(account);
    }

    public async Task<AccountSnapshot?> FindByIdAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var account = await users.FindByIdAsync(accountId.ToString());

        return account is null ? null : ToSnapshot(account);
    }

    public async Task<AccountSnapshot?> CreateAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var account = new AccountDao
        {
            Id = Guid.CreateVersion7(),
            // An account here is an address and a password. There is no display
            // name, so the user name the identity component insists on is the
            // address itself.
            UserName = email,
            Email = email,
            EmailConfirmed = false,
            LockoutEnabled = true,
            CreatedAt = clock.GetUtcNow(),
        };

        IdentityResult result;

        try
        {
            result = await users.CreateAsync(account, password);
        }
        catch (DbUpdateException)
        {
            // The identity component checks for a duplicate address by reading
            // before it writes, which two simultaneous registrations both pass.
            // The unique index is what actually settles it, and this is that
            // index refusing the second insert. FR-006, and it is the only
            // reason the requirement holds under a double submission.
            return null;
        }

        if (result.Succeeded)
        {
            return ToSnapshot(account);
        }

        if (result.Errors.Any(IsDuplicate))
        {
            return null;
        }

        // The password was checked against the policy before this was called, so
        // anything else is a mistake in the wiring rather than in the submission,
        // and hiding it would make it harder to find.
        throw new InvalidOperationException(
            "Creating an account failed for a reason that is not a duplicate address: "
                + string.Join(", ", result.Errors.Select(error => error.Code)));
    }

    public async Task<bool> VerifyPasswordAsync(
        Guid accountId,
        string password,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (await users.FindByIdAsync(accountId.ToString()) is not { } account)
        {
            return false;
        }

        if (await users.CheckPasswordAsync(account, password))
        {
            await users.ResetAccessFailedCountAsync(account);
            return true;
        }

        // Counting the failure is what eventually applies the lockout. FR-016.
        await users.AccessFailedAsync(account);
        return false;
    }

    public Task BurnPasswordVerificationAsync(string password, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // The result is deliberately discarded. The point is the work, not the
        // answer: without it an unknown address returns before any hashing has
        // happened and is measurably faster than a wrong password.
        Hasher.VerifyHashedPassword(new AccountDao(), DummyHash.Value, password);

        return Task.CompletedTask;
    }

    public async Task ConfirmEmailAsync(Guid accountId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (await users.FindByIdAsync(accountId.ToString()) is not { } account)
        {
            return;
        }

        if (account.EmailConfirmed)
        {
            return;
        }

        account.EmailConfirmed = true;
        await users.UpdateAsync(account);
    }

    public async Task<RecoveryOutcome> SetPasswordAsync(
        Guid accountId,
        string token,
        string password,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (await users.FindByIdAsync(accountId.ToString()) is not { } account)
        {
            // Indistinguishable from a bad token, which is the point: an account
            // id taken from a link must not reveal whether it names anything.
            return RecoveryOutcome.LinkExpired;
        }

        // Validates the token and replaces the hash in one step. Replacing the
        // hash rotates the security stamp, which is what ends every session that
        // existed beforehand and makes this same token unredeemable. FR-022 and
        // FR-024, both for free.
        var result = await users.ResetPasswordAsync(account, token, password);

        if (!result.Succeeded)
        {
            return result.Errors.Any(error => error.Code == "InvalidToken")
                ? RecoveryOutcome.LinkExpired
                : RecoveryOutcome.PasswordRejected;
        }

        // FR-021: a lockout is what sent many people here, so leaving it in place
        // would mean recovering an account and still being unable to use it.
        await users.SetLockoutEndDateAsync(account, null);
        await users.ResetAccessFailedCountAsync(account);

        // FR-023: following a link sent to the address proves what confirmation
        // proves, so an account that was never confirmed is confirmed now rather
        // than left in a state it cannot leave.
        if (!account.EmailConfirmed)
        {
            account.EmailConfirmed = true;
            await users.UpdateAsync(account);
        }

        return RecoveryOutcome.PasswordSet;
    }

    public async Task EndAllSessionsAsync(Guid accountId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (await users.FindByIdAsync(accountId.ToString()) is { } account)
        {
            await users.UpdateSecurityStampAsync(account);
        }
    }

    private static bool IsDuplicate(IdentityError error) =>
        error.Code is "DuplicateEmail" or "DuplicateUserName";

    private static AccountSnapshot ToSnapshot(AccountDao account) =>
        new(
            account.Id,
            account.Email ?? string.Empty,
            account.EmailConfirmed,
            account.IsVerifiedBeneficiary,
            account.LockoutEnd);
}
