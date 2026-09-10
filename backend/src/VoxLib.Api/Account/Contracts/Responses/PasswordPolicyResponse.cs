namespace VoxLib.Api.Account.Contracts.Responses;

/// <summary>
/// What a password must satisfy, served so the interface states the same numbers
/// the API enforces. FR-002 asks for the requirements to be available before the
/// form is submitted, and the alternative is the same numbers written twice, in
/// two languages, drifting the first time either changes.
/// </summary>
public sealed record PasswordPolicyResponse(
    int MinimumLength,
    bool RequiresDigit,
    bool RequiresUppercase,
    bool RequiresLowercase,
    bool RequiresNonAlphanumeric);
