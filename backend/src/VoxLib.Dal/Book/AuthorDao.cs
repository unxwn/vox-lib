namespace VoxLib.Dal.Book;

public sealed class AuthorDao
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string SortName { get; set; } = string.Empty;

    public List<BookAuthorDao> Credits { get; set; } = [];
}
