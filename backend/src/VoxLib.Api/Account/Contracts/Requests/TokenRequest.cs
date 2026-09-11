namespace VoxLib.Api.Account.Contracts.Requests;

/// <summary>
/// The two halves of a link that was sent to an address: which account it is
/// for, and the proof that the person controls that address.
/// </summary>
public sealed record TokenRequest(Guid AccountId, string? Token);
