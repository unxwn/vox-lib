namespace VoxLib.Model.Book;

/// <summary>
/// An audiobook in the catalogue. The rules that decide what a reader may see
/// live here rather than in a service, so no caller can leak an unpublished book
/// by forgetting a filter.
/// </summary>
public sealed class Book
{
    /// <summary>
    /// The language a book's own metadata is written in, when nothing else says
    /// otherwise. FR-018 needs this so a screen reader picks the right voice.
    /// </summary>
    public const string DefaultLanguage = "uk";

    /// <summary>Identity. Never appears in an address.</summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Unique, lowercase and address-safe. This is what carries the title into
    /// search results, and it is stored rather than derived so that correcting a
    /// title later does not break every existing link.
    /// </summary>
    public required string Slug { get; init; }

    /// <summary>Sorted under Ukrainian collation, per FR-015.</summary>
    public required string Title { get; init; }

    /// <summary>Optional. A published book may have none.</summary>
    public string? Description { get; init; }

    /// <summary>Optional. Absent means the placeholder is shown, per FR-006.</summary>
    public string? CoverArtUrl { get; init; }

    /// <summary>Language tag for this book's metadata, so its page can declare it.</summary>
    public string Language { get; init; } = DefaultLanguage;

    public required PublicationState PublicationState { get; init; }

    /// <summary>At least one for a published book.</summary>
    public required IReadOnlyList<Author> Authors { get; init; }

    /// <summary>Ordered by <see cref="Chapter.Position"/>. May be empty.</summary>
    public required IReadOnlyList<Chapter> Chapters { get; init; }

    /// <summary>
    /// Whether this book may appear anywhere a reader can see. Every read path
    /// goes through it.
    /// </summary>
    public bool IsListed => PublicationState == PublicationState.Published;

    public int ChapterCount => Chapters.Count;

    /// <summary>Zero when the book has no chapters yet, rather than undefined.</summary>
    public TimeSpan TotalRunningTime =>
        Chapters.Aggregate(TimeSpan.Zero, static (total, chapter) => total + chapter.RunningTime);
}
