namespace VoxLib.Model.Book;

/// <summary>
/// One phrase worth searching for, drawn from the published catalogue.
/// <para>
/// It serves twice: as a link that runs the search it names, and as one frame of
/// the rotating example inside the untouched field.
/// </para>
/// </summary>
/// <param name="Term">
/// Verbatim, and that is the whole of FR-023's guarantee. Activating a
/// suggestion always returns at least one result only because the string is
/// never shortened, prettified or case-folded on the way out: it is a published
/// book's title or a credited author's name, exactly as stored, so searching for
/// it necessarily matches the row it came from.
/// </param>
/// <param name="Kind">What it was drawn from, so the link can be announced precisely.</param>
public sealed record SearchSuggestion(string Term, SuggestionKind Kind);

public enum SuggestionKind
{
    Title,
    Author,
}
