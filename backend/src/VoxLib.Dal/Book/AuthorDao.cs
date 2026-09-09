namespace VoxLib.Dal.Book;

public sealed class AuthorDao
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public List<BookDao> Books { get; set; } = [];
}
