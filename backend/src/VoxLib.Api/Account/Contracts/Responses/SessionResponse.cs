namespace VoxLib.Api.Account.Contracts.Responses;

/// <summary>
/// Who this browser is signed in as. Always answered, with
/// <c>signedIn</c> false rather than a refusal when nobody is, because this is
/// how every page decides what to show and how the interface notices a browser
/// that is discarding the session.
/// </summary>
public sealed record SessionResponse(bool SignedIn, string? Email, bool IsVerifiedBeneficiary)
{
    public static readonly SessionResponse Anonymous = new(false, null, false);
}
