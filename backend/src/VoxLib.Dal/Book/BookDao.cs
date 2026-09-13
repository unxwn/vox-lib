namespace VoxLib.Dal.Book;

/// <summary>
/// The persistence shape of a book. Separate from the domain entity by design:
/// the domain carries behaviour and no storage concerns, and the two are mapped
/// exactly once, here in VoxLib.Dal.
/// </summary>
public sealed class BookDao
{
    public Guid Id { get; set; }

    public string Slug { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? CoverKey { get; set; }

    public string? Narrator { get; set; }

    public string Language { get; set; } = Model.Book.Book.DefaultLanguage;

    public Model.Book.PublicationState PublicationState { get; set; }

    public DateTimeOffset AddedToCatalogue { get; set; }

    public List<BookAuthorDao> Credits { get; set; } = [];

    public List<ChapterDao> Chapters { get; set; } = [];
}
