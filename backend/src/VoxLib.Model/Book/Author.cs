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
}
