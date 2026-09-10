namespace VoxLib.Api.Account.Contracts.Requests;

/// <summary>
/// A recovery link plus the password to set. The account id and token come from
/// the link that was sent to the address; the password comes from the person.
/// </summary>
public sealed record PasswordResetRequest(Guid AccountId, string? Token, string? Password);
