namespace VoxLib.Api.Account.Contracts.Requests;

/// <summary>An address and a password. An account in this product is nothing else.</summary>
public sealed record RegistrationRequest(string? Email, string? Password);
