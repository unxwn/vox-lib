using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using VoxLib.Dal.Book;
using VoxLib.Dal.Persistence;
using VoxLib.Model.Book;

namespace VoxLib.Api.Tests.Infrastructure;

/// <summary>
/// A catalogue big enough and varied enough to have behaviour, seeded by a
/// fixture rather than shipped.
/// <para>
/// It exists because the catalogue the product actually holds is four books:
/// nothing paginates, no two titles collide, no book is missing a description,
/// and only two letters of the alphabet are represented. Every one of those is
/// something the catalogue is required to get right, so the assertions stay and
/// the books they need are seeded here. Putting them in <c>books.json</c>
/// instead would mean shipping test data to readers.
/// </para>
/// </summary>
public static class SampleCatalogue
{
    public const int PageSize = 20;

    public const int PublishedCount = 26;

    public const int PageCount = 2;

    /// <summary>Seeded but not published, so nothing may ever show it.</summary>
    public const string DraftSlug = "chorna-rada";

    /// <summary>
    /// Every published book, in the order the catalogue must present them:
    /// by title under Ukrainian collation, settled by slug.
    /// <para>
    /// This order is the point of several requirements at once, so it is worth
    /// reading rather than skimming. Under a naive byte ordering "Ґудзик" would
    /// fall at the end of the catalogue rather than after "Гайдамаки", and
    /// "Єретик", "Інтермеццо" and "Їжачок" would fall before "Апостол черні",
    /// because those letters sit outside the contiguous Cyrillic run. Latin
    /// sorts after Cyrillic, which is why the English title is last.
    /// </para>
    /// </summary>
    public static readonly string[] SlugsInOrder =
    [
        "apostol-cherni", // А
        "boiarynia", // Б
        "vershnyky", // В
        "haidamaky", // Г
        "gudzyk", // Ґ, immediately after Г and not at the end
        "dim-na-hori", // Д
        "eneida", // Е
        "yeretyk", // Є, after Е and not before А
        "zhovtyi-kniaz", // Ж
        "zemlia", // З
        "intermezzo", // І
        "yizhachok-i-zymova-kazka", // Ї, after І
        "kaidasheva-simia", // К
        "lisova-pisnia", // Л
        "misto", // М
        "natalka-poltavka", // Н
        "opovidannia-vovchok", // О
        "podorozh-doktora-leonardo", // П
        "roksolana", // Р
        "son-shevchenko", // С, last on page one
        "son-vovchok", // the same title, first on page two
        "tini-zabutykh-predkiv", // Т
        "ukradene-shchastia", // У
        "fata-morgana", // Ф
        "khiba-revut-voly", // Х
        "kobzar-selected-poems", // Latin, after every Cyrillic title
    ];

    /// <summary>
    /// The two books that share a title, straddling the page boundary. Together
    /// they are what proves the order is total: sorted by title alone, nothing
    /// decides which of them page one ends with.
    /// </summary>
    public const string TitleSharedByTwoBooks = "Сон";

    public const string LastSlugOnFirstPage = "son-shevchenko";

    public const string FirstSlugOnSecondPage = "son-vovchok";

    /// <summary>Published, and deliberately without cover art, per FR-046.</summary>
    public const string SlugWithoutCoverArt = "gudzyk";

    /// <summary>Published, and deliberately without a description.</summary>
    public const string SlugWithoutDescription = "yeretyk";

    /// <summary>Published with no chapters yet, so its running time is zero.</summary>
    public const string SlugWithoutChapters = "intermezzo";

    /// <summary>Published, with metadata in a language other than Ukrainian.</summary>
    public const string SlugInAnotherLanguage = "kobzar-selected-poems";

    /// <summary>Published, and credited to two people, so co-authorship is exercised.</summary>
    public const string SlugWithTwoAuthors = "khiba-revut-voly";

    /// <summary>Published, and the one book carrying cover art.</summary>
    public const string SlugWithCoverArt = "boiarynia";

    private const string ResourceName =
        "VoxLib.Api.Tests.Infrastructure.sample-catalogue.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>
    /// Replaces whatever the application seeded with this catalogue, so the
    /// description above is the whole truth about what the tests are reading.
    /// </summary>
    public static async Task ReplaceCatalogueAsync(VoxLibDbContext database)
    {
        // Chapters and the credit rows go with the books, by cascade.
        database.Books.RemoveRange(database.Books);
        database.Authors.RemoveRange(database.Authors);
        await database.SaveChangesAsync();

        var content = Read();

        var authorsByName = content.Authors.ToDictionary(
            author => author.Name,
            author => new AuthorDao
            {
                Id = Guid.CreateVersion7(),
                Name = author.Name,
                // The rule, unless the fixture settles it by hand. One pair
                // here is the same person spelled two ways and transliterates
                // alike; the shipped seed refuses such a collision, and this
                // data keeps both spellings because a Latin-spelled author is
                // what proves case-insensitive matching works for Latin too.
                Slug = author.Slug ?? AuthorSlug.From(author.Name),
                SortName = author.SortName,
            },
            StringComparer.Ordinal);

        var addedToCatalogue = DateTimeOffset.UtcNow;

        foreach (var seed in content.Books)
        {
            var bookId = Guid.CreateVersion7();

            database.Books.Add(
                new BookDao
                {
                    Id = bookId,
                    Slug = seed.Slug,
                    Title = seed.Title,
                    Description = seed.Description,
                    CoverKey = seed.CoverKey,
                    Narrator = seed.Narrator,
                    Language = seed.Language,
                    PublicationState = seed.PublicationState,
                    AddedToCatalogue = addedToCatalogue,
                    Credits =
                    [
                        .. seed.Authors.Select(credit => new BookAuthorDao
                        {
                            BookId = bookId,
                            Author = authorsByName[credit.Name],
                            Role = credit.Role,
                        }),
                    ],
                    Chapters =
                    [
                        .. seed.Chapters.Select(chapter => new ChapterDao
                        {
                            Id = Guid.CreateVersion7(),
                            BookId = bookId,
                            Title = chapter.Title,
                            Position = chapter.Position,
                            RunningTime = TimeSpan.FromSeconds(chapter.RunningTimeSeconds),
                        }),
                    ],
                });
        }

        await database.SaveChangesAsync();
    }

    private static SampleContent Read()
    {
        using var stream =
            Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"'{ResourceName}' is not embedded.");

        return JsonSerializer.Deserialize<SampleContent>(stream, SerializerOptions)
            ?? throw new InvalidOperationException($"'{ResourceName}' is empty or malformed.");
    }

    private sealed record SampleContent(
        IReadOnlyList<SampleAuthor> Authors,
        IReadOnlyList<SampleBook> Books);

    private sealed record SampleAuthor(string Name, string? Slug, string SortName);

    private sealed record SampleBook(
        string Slug,
        string Title,
        string? Description,
        string? CoverKey,
        string? Narrator,
        string Language,
        PublicationState PublicationState,
        IReadOnlyList<SampleCredit> Authors,
        IReadOnlyList<SampleChapter> Chapters);

    private sealed record SampleCredit(string Name, CreditRole Role);

    private sealed record SampleChapter(int Position, string Title, int RunningTimeSeconds);
}
