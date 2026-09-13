using VoxLib.Model.Book;

namespace VoxLib.Dal.Book;

/// <summary>
/// The join between a book and someone credited on it, made explicit so it can
/// carry the role. It was a shadow entity until the role needed somewhere to
/// live, and the role has nowhere else to live: see <see cref="AuthorCredit"/>.
/// </summary>
public sealed class BookAuthorDao
{
    public Guid BookId { get; set; }

    public BookDao? Book { get; set; }

    public Guid AuthorId { get; set; }

    public AuthorDao? Author { get; set; }

    public CreditRole Role { get; set; }
}
