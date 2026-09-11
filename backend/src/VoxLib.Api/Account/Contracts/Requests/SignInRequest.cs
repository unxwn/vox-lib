namespace VoxLib.Api.Account.Contracts.Requests;

/// <summary>The address and password given at the sign-in form.</summary>
public sealed record SignInRequest(string? Email, string? Password);
