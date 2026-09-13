using VoxLib.Model.Book;

namespace VoxLib.Api.Search.Contracts.Responses;

/// <param name="Term">
/// Verbatim, never edited. Searching for exactly this string returns at least
/// one result, which only holds because it was taken from a published row
/// unchanged.
/// </param>
public sealed record SearchSuggestion(string Term, SuggestionKind Kind);

/// <summary>
/// An object with one array rather than a bare array, so the response has
/// somewhere to grow without becoming a different shape.
/// </summary>
public sealed record SearchSuggestions(IReadOnlyList<SearchSuggestion> Items);
