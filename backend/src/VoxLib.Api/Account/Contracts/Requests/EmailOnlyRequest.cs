namespace VoxLib.Api.Account.Contracts.Requests;

/// <summary>
/// Asking for a message to be sent again: a confirmation, or a way to set a new
/// password. Both answer the same whether or not the address has an account.
/// </summary>
public sealed record EmailOnlyRequest(string? Email);
