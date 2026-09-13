using VoxLib.Model.Book;
using DomainAuthor = VoxLib.Model.Book.Author;
using DomainBook = VoxLib.Model.Book.Book;
using DomainChapter = VoxLib.Model.Book.Chapter;

namespace VoxLib.Dal.Book;

/// <summary>
/// The one crossing from storage to the domain. Keeping it in a single place is
/// what lets the rest of the application forget that a database exists.
/// </summary>
public static class BookMapping
{
    public static DomainBook ToDomain(this BookDao book) =>
        new()
        {
            Id = book.Id,
            Slug = book.Slug,
            Title = book.Title,
            Description = book.Description,
            CoverKey = book.CoverKey,
            Narrator = book.Narrator,
            Language = book.Language,
            PublicationState = book.PublicationState,
            AddedToCatalogue = book.AddedToCatalogue,

            // The join carries no order of its own, so one is imposed here.
            // Ordering by name is a decision, not a requirement: the
            // specification does not say how co-authors should be presented, and
            // a book whose first author is its principal one would need a stored
            // order to say so.
            Authors =
            [
                .. book
                    .Credits.Where(credit => credit.Author is not null)
                    .OrderBy(credit => credit.Author!.Name, StringComparer.Ordinal)
                    .Select(credit => new AuthorCredit(credit.Author!.ToDomain(), credit.Role)),
            ],

            Chapters =
            [
                .. book
                    .Chapters.OrderBy(chapter => chapter.Position)
                    .Select(chapter => new DomainChapter
                    {
                        Id = chapter.Id,
                        BookId = chapter.BookId,
                        Title = chapter.Title,
                        Position = chapter.Position,
                        RunningTime = chapter.RunningTime,
                    }),
            ],
        };

    public static DomainAuthor ToDomain(this AuthorDao author) =>
        new()
        {
            Id = author.Id,
            Name = author.Name,
            Slug = author.Slug,
            SortName = author.SortName,
        };
}
