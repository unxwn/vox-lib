namespace VoxLib.Orchestrator.Account;

/// <summary>
/// Where the site lives, for the links inside messages.
/// <para>
/// A link in a message has no request to be relative to, so this is the one
/// place in the product an origin is configured. Everything the browser calls
/// stays relative, because hardcoding an origin there breaks the dev proxy and
/// brings CORS back.
/// </para>
/// </summary>
public sealed record SiteAddress(string BaseAddress)
{
    public string Link(string path, Guid accountId, string token) =>
        $"{BaseAddress.TrimEnd('/')}/{path.TrimStart('/')}"
            + $"?account={accountId}&token={Uri.EscapeDataString(token)}";
}
