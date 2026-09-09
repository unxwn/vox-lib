namespace VoxLib.Api.Book.Contracts.Responses;

/// <summary>
/// One book as its own page shows it. Matches BookDetail in
/// specs/001-browse-catalogue/contracts/catalogue.yaml, which is what the
/// frontend's types are generated from.
/// <para>
/// It carries no audio field, no link to one and no storage key, and it never
/// will: FR-010 holds on the detail view exactly as it does on the listing, and
/// this is the response where the temptation would be.
/// </para>
/// </summary>
/// <param name="Slug">The book's stable, address-safe name.</param>
/// <param name="Description">Null when the book has none.</param>
/// <param name="CoverArtUrl">Null when the book has no cover art.</param>
/// <param name="Language">
/// The language tag its metadata is written in, so the page can declare it and a
/// screen reader reads the book in the right voice. FR-018.
/// </param>
/// <param name="TotalRunningTimeSeconds">Zero when the book has no chapters yet.</param>
/// <param name="Chapters">In position order. Empty when none have been added.</param>
public sealed record BookDetail(
    string Slug,
    string Title,
    IReadOnlyList<string> Authors,
    string? Description,
    string? CoverArtUrl,
    string Language,
    int ChapterCount,
    int TotalRunningTimeSeconds,
    IReadOnlyList<Chapter> Chapters);
