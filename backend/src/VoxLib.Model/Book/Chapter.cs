namespace VoxLib.Model.Book;

/// <summary>
/// One part of a book. Carries no audio: not a file location, not a storage key,
/// nothing that could be mistaken for one. FR-010 is a property of this model
/// rather than something the endpoints have to remember to strip.
/// </summary>
public sealed class Chapter
{
    public required Guid Id { get; init; }

    /// <summary>The owning book. A chapter belongs to exactly one.</summary>
    public required Guid BookId { get; init; }

    public required string Title { get; init; }

    /// <summary>Starts at 1 and is unique within a book.</summary>
    public required int Position { get; init; }

    public required TimeSpan RunningTime { get; init; }
}
