namespace VoxLib.Dal.Book;

public sealed class ChapterDao
{
    public Guid Id { get; set; }

    public Guid BookId { get; set; }

    public BookDao? Book { get; set; }

    public string Title { get; set; } = string.Empty;

    public int Position { get; set; }

    public TimeSpan RunningTime { get; set; }
}
