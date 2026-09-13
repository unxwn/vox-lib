using VoxLib.Model.Book;

namespace VoxLib.Orchestrator.Book;

/// <summary>
/// Which phrases the search field offers as examples.
/// <para>
/// The rule lives here rather than in the handler because FR-023's guarantee —
/// activating a suggestion always returns at least one result — is a property of
/// the rule and not of the transport. It holds because of one thing: <b>every
/// term is a published row's own text, copied and never edited.</b> Shorten a
/// title to fit, strip its punctuation, or title-case an author's name, and the
/// search that follows can match nothing, which is the one thing a suggestion
/// must never do.
/// </para>
/// </summary>
public sealed class CatalogueSuggestions(IBookRepository books, IAuthorRepository authors)
    : ICatalogueSuggestions
{
    /// <summary>At most five, which is as many as fit beneath a field without becoming a list.</summary>
    public const int MaxSuggestions = 5;

    public async Task<IReadOnlyList<SearchSuggestion>> SampleAsync(
        CancellationToken cancellationToken)
    {
        // Split half and half, rounded towards titles: a visitor searching a
        // catalogue is more often after a book than after a person, and an odd
        // number has to round somewhere.
        var titleCount = (MaxSuggestions + 1) / 2;
        var authorCount = MaxSuggestions - titleCount;

        var sampledTitles = await books.SampleTitlesAsync(titleCount, cancellationToken);
        var sampledNames = await authors.SampleNamesAsync(authorCount, cancellationToken);

        return
        [
            .. sampledTitles.Select(title => new SearchSuggestion(title, SuggestionKind.Title)),
            .. sampledNames.Select(name => new SearchSuggestion(name, SuggestionKind.Author)),
        ];
    }
}
