namespace VoxLib.Api.Account.Contracts.Responses;

/// <summary>
/// How confirming ended. <c>alreadyConfirmed</c> is a success rather than an
/// error, because following the same link twice is ordinary and calling it a
/// failure sends a person looking for a problem that is not there. FR-008.
/// </summary>
public sealed record ConfirmationResult(string Outcome)
{
    public static readonly ConfirmationResult Confirmed = new("confirmed");

    public static readonly ConfirmationResult AlreadyConfirmed = new("alreadyConfirmed");
}
