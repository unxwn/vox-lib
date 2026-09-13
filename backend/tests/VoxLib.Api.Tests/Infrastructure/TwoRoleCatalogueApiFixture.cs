using VoxLib.Dal.Book;
using VoxLib.Dal.Persistence;
using VoxLib.Model.Book;

namespace VoxLib.Api.Tests.Infrastructure;

/// <summary>
/// The shipped catalogue plus one book that credits an existing compiler as an
/// author, so that one person holds two different roles.
/// <para>
/// Seeded here rather than in <c>books.json</c>, which holds only what the
/// project actually has. The pairing is the thing under test and no real book
/// exercises it yet, so inventing one for readers to see would be the wrong
/// trade.
/// </para>
/// </summary>
public sealed class TwoRoleCatalogueApiFixture : ApiFixture
{
    /// <summary>The book this fixture adds, credited to the compiler as an author.</summary>
    public const string SecondBookSlug = "druha-knyzhka-tsiiei-liudyny";

    protected override async Task PrepareAsync(VoxLibDbContext database)
    {
        var person =
            database.Authors.SingleOrDefault(author =>
                author.Slug == SeededCatalogue.CompilerSlug)
            ?? throw new InvalidOperationException(
                $"The seed no longer holds '{SeededCatalogue.CompilerSlug}'.");

        var bookId = Guid.CreateVersion7();

        database.Books.Add(
            new BookDao
            {
                Id = bookId,
                Slug = SecondBookSlug,
                Title = "Друга книжка цієї людини",
                Description = "Написана, а не впорядкована.",
                Language = "uk",
                PublicationState = PublicationState.Published,
                AddedToCatalogue = DateTimeOffset.UtcNow,
                Credits =
                [
                    new BookAuthorDao
                    {
                        BookId = bookId,
                        AuthorId = person.Id,
                        Role = CreditRole.Author,
                    },
                ],
                Chapters =
                [
                    new ChapterDao
                    {
                        Id = Guid.CreateVersion7(),
                        BookId = bookId,
                        Title = "Повний запис",
                        Position = 1,
                        RunningTime = TimeSpan.FromMinutes(30),
                    },
                ],
            });

        await database.SaveChangesAsync();
    }
}
