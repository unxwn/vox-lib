using VoxLib.Dal.Book;
using VoxLib.Dal.Persistence;
using VoxLib.Model.Book;

namespace VoxLib.Api.Tests.Infrastructure;

/// <summary>
/// The shipped catalogue plus one book that records who reads it.
/// <para>
/// Seeded here rather than in <c>books.json</c> because no real book has a
/// narrator: <c>content.md</c> records no озвучувач for any of the four. That is
/// the honest state of FR-040 in this feature — the field is added now because
/// it cannot be backfilled honestly later, and this fixture is the only thing
/// that proves it works at all.
/// </para>
/// </summary>
public sealed class NarratedBookFixture : ApiFixture
{
    public const string NarratedSlug = "knyzhka-z-ozvuchuvachem";

    public const string NarratorName = "Олена Читальниця";

    protected override async Task PrepareAsync(VoxLibDbContext database)
    {
        var person =
            database.Authors.FirstOrDefault()
            ?? throw new InvalidOperationException("The seed left no authors to credit.");

        var bookId = Guid.CreateVersion7();

        database.Books.Add(
            new BookDao
            {
                Id = bookId,
                Slug = NarratedSlug,
                Title = "Книжка з озвучувачем",
                Language = "uk",
                PublicationState = PublicationState.Published,
                AddedToCatalogue = DateTimeOffset.UtcNow,
                Narrator = NarratorName,
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
                        RunningTime = TimeSpan.FromMinutes(45),
                    },
                ],
            });

        await database.SaveChangesAsync();
    }
}
