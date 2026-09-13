namespace VoxLib.Model.Book;

/// <summary>
/// Someone credited on a book. An author is credited on many books, and a book
/// credits one or more authors.
/// </summary>
public sealed class Author
{
    public required Guid Id { get; init; }

    /// <summary>Sorted and matched under Ukrainian collation, per FR-015.</summary>
    public required string Name { get; init; }

    /// <summary>
    /// Unique, and the author's address. Derived from <see cref="Name"/> by
    /// <see cref="AuthorSlug.From"/> once, when the row is created, and never
    /// again: FR-038 requires that correcting a spelling leave the address alone.
    /// </summary>
    public required string Slug { get; init; }

    /// <summary>
    /// The name in <c>Прізвище, Ім'я</c> form, which is what the index is
    /// ordered by. Authored rather than derived: no rule separates a surname
    /// from a given name reliably enough to guess with.
    /// </summary>
    public required string SortName { get; init; }
}
