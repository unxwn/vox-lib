namespace VoxLib.Api.Book.Contracts.Responses;

/// <summary>
/// One book as it appears in a list. Matches BookSummary in
/// specs/001-browse-catalogue/contracts/catalogue.yaml, which is what the
/// frontend's types are generated from.
/// </summary>
/// <param name="Slug">The book's stable, address-safe name.</param>
/// <param name="CoverArtUrl">Null when the book has no cover art.</param>
/// <param name="TotalRunningTimeSeconds">Zero when the book has no chapters yet.</param>
public sealed record BookSummary(
    string Slug,
    string Title,
    IReadOnlyList<string> Authors,
    string? CoverArtUrl,
    int ChapterCount,
    int TotalRunningTimeSeconds);
