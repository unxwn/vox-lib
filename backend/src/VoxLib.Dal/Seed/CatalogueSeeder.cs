using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using VoxLib.Dal.Book;
using VoxLib.Dal.Persistence;
using VoxLib.Model.Book;

namespace VoxLib.Dal.Seed;

/// <summary>
/// Puts the committed catalogue into an empty database. Content arrives only by
/// seeding until the administrative feature lands, and seeding a fresh container
/// and a test run from the same file is what makes them agree.
/// <para>
/// People come from <c>authors.json</c> rather than from the distinct names in
/// <c>books.json</c>, because a person now carries a slug and a sort name that
/// cannot be derived from a credit. A book naming someone that file does not
/// hold is an error rather than a new row: silently inventing the person is how
/// a misspelt credit becomes a second author page nobody notices.
/// </para>
/// </summary>
public sealed class CatalogueSeeder(VoxLibDbContext database)
{
    private const string BooksResource = "VoxLib.Dal.Seed.books.json";
    private const string AuthorsResource = "VoxLib.Dal.Seed.authors.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>
    /// Seeds only when the catalogue is empty, so running it again is harmless
    /// and a database someone has since edited is left alone.
    /// </summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await database.Books.AnyAsync(cancellationToken))
        {
            return;
        }

        var seedAuthors = Read<SeedAuthor>(AuthorsResource);
        var seedBooks = Read<SeedBook>(BooksResource);

        var authorsByName = BuildAuthors(seedAuthors);
        var addedToCatalogue = DateTimeOffset.UtcNow;

        foreach (var seed in seedBooks)
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
                            Author = Credited(authorsByName, credit.Name, seed.Slug),
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

        await database.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// One row per person, with the slug taken from the domain's own rule rather
    /// than from the file, so the two can never disagree.
    /// </summary>
    private static Dictionary<string, AuthorDao> BuildAuthors(
        IReadOnlyList<SeedAuthor> seedAuthors)
    {
        var byName = new Dictionary<string, AuthorDao>(StringComparer.Ordinal);
        var bySlug = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var seed in seedAuthors)
        {
            var slug = AuthorSlug.From(seed.Name);

            // The file records the slug as well, and is checked against the rule
            // rather than trusted: the value is what the address will be, so a
            // hand-edited one that no longer matches the name is worth failing
            // on here rather than discovering as a dead link.
            if (!string.Equals(slug, seed.Slug, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"authors.json records the slug '{seed.Slug}' for '{seed.Name}', "
                        + $"but the name transliterates to '{slug}'.");
            }

            // A collision is an error, not something to resolve with a number.
            // An automatic suffix turns two rows for one person into two
            // addresses and buries the defect, which is what FR-038 forbids.
            if (bySlug.TryGetValue(slug, out var owner))
            {
                throw new InvalidOperationException(
                    $"The authors '{owner}' and '{seed.Name}' both slugify to '{slug}'. "
                        + "Author slugs must be unique; resolve the collision in authors.json.");
            }

            bySlug[slug] = seed.Name;
            byName[seed.Name] = new AuthorDao
            {
                Id = Guid.CreateVersion7(),
                Name = seed.Name,
                Slug = slug,
                SortName = seed.SortName,
            };
        }

        return byName;
    }

    private static AuthorDao Credited(
        Dictionary<string, AuthorDao> authorsByName,
        string name,
        string bookSlug) =>
        authorsByName.TryGetValue(name, out var author)
            ? author
            : throw new InvalidOperationException(
                $"The book '{bookSlug}' credits '{name}', who is not in authors.json. "
                    + "Add the person there, with a sort name, before crediting them.");

    private static IReadOnlyList<T> Read<T>(string resourceName)
    {
        using var stream =
            Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"The seed dataset '{resourceName}' is not embedded in the assembly.");

        return JsonSerializer.Deserialize<List<T>>(stream, SerializerOptions)
            ?? throw new InvalidOperationException($"'{resourceName}' is empty or malformed.");
    }

    private sealed record SeedAuthor(string Name, string Slug, string SortName);

    private sealed record SeedBook(
        string Slug,
        string Title,
        string? Description,
        string? CoverKey,
        string? Narrator,
        string Language,
        PublicationState PublicationState,
        IReadOnlyList<SeedCredit> Authors,
        IReadOnlyList<SeedChapter> Chapters);

    private sealed record SeedCredit(string Name, CreditRole Role);

    private sealed record SeedChapter(int Position, string Title, int RunningTimeSeconds);
}
