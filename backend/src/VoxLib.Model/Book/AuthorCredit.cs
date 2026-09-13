namespace VoxLib.Model.Book;

/// <summary>
/// One person's credit on one book. The role belongs to the pairing rather than
/// to either end of it: on the author it would say that a compiler <em>is</em> a
/// compiler, which stops being true the first time they write something; on the
/// book it would say a book has one kind of credit, which stops being true the
/// first time one has both an author and a translator.
/// </summary>
/// <param name="Author">The person credited.</param>
/// <param name="Role">How they are credited on this book.</param>
public sealed record AuthorCredit(Author Author, CreditRole Role);
