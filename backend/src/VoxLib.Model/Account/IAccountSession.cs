namespace VoxLib.Model.Account;

/// <summary>
/// The current browser's session. Implemented in VoxLib.Api, because writing the
/// session needs the request it belongs to, and that is a transport concern
/// rather than a rule. The interface is declared here so the orchestrator names
/// the intent and never the mechanism.
/// </summary>
public interface IAccountSession
{
    /// <summary>
    /// Starts a session that survives closing the browser, and is held so that
    /// scripts running in the page cannot read it. FR-012 and FR-013.
    /// </summary>
    Task StartAsync(Guid accountId, CancellationToken cancellationToken);

    /// <summary>
    /// Ends this browser's session, leaving any session on another device alone.
    /// FR-018.
    /// </summary>
    Task EndAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Who this request belongs to, or <see cref="Listener.Anonymous"/>. Never
    /// null, so a caller cannot forget the anonymous case.
    /// </summary>
    Listener Current { get; }
}
