using VoxLib.Api.Author.Contracts.Responses;

namespace VoxLib.Api.Book.Contracts.Responses;

/// <summary>
/// One book as it appears in a list. Matches BookSummary in
/// specs/004-site-shell/contracts/catalogue.yaml, which is what the frontend's
/// types are generated from.
/// </summary>
/// <param name="Slug">The book's stable, address-safe name.</param>
/// <param name="Authors">
/// References rather than bare names, so a credit can become a link and say how
/// the person is credited without a second request (FR-033, FR-066).
/// </param>
/// <param name="Cover">Null when the book has no cover, which is a designed
/// state: the interface shows the placeholder rather than nothing.</param>
/// <param name="TotalRunningTimeSeconds">Zero when the book has no chapters yet.</param>
public sealed record BookSummary(
    string Slug,
    string Title,
    IReadOnlyList<AuthorReference> Authors,
    Cover? Cover,
    int ChapterCount,
    int TotalRunningTimeSeconds);
