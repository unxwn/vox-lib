namespace VoxLib.Model.Book;

/// <summary>
/// A small sample of things worth searching for, drawn fresh from the published
/// catalogue. Implemented in VoxLib.Orchestrator, because the sampling rule is a
/// business rule rather than a detail of a handler: FR-023's guarantee is a
/// property of that rule, not of the endpoint that serves it.
/// </summary>
public interface ICatalogueSuggestions
{
    /// <summary>
    /// At most five, sampled fresh on each call. An empty list when the
    /// catalogue is empty, which is a state rather than an error: the field
    /// falls back to a neutral placeholder and stays fully usable.
    /// </summary>
    Task<IReadOnlyList<SearchSuggestion>> SampleAsync(CancellationToken cancellationToken);
}
