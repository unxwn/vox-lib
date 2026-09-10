namespace VoxLib.Dal.Account;

/// <summary>
/// One browser's session. The cookie holds <see cref="Id"/> and nothing else, so
/// deleting this row is what ends that session and only that session.
/// </summary>
public sealed class SessionDao
{
    public required string Id { get; set; }

    /// <summary>
    /// The encrypted session, opaque to this layer. Storing it rather than the
    /// account id keeps the decision about what a session contains where it
    /// belongs.
    /// </summary>
    public required byte[] Payload { get; set; }

    public required DateTimeOffset ExpiresAt { get; set; }
}
