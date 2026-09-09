using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using VoxLib.Dal.Book;
using VoxLib.Dal.Persistence;

namespace VoxLib.Dal.Seed;

/// <summary>
/// Puts the committed catalogue into an empty database. Content arrives only by
/// seeding until the administrative feature lands, and seeding a fresh container
/// and a test run from the same file is what makes them agree.
/// </summary>
public sealed class CatalogueSeeder(VoxLibDbContext database)
{
    private const string ResourceName = "VoxLib.Dal.Seed.books.json";

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

        var seedBooks = Read();

        // One AuthorDao per distinct name, so a writer credited on several books
        // is one row rather than several.
        var authorsByName = seedBooks
            .SelectMany(book => book.Authors)
            .Distinct(StringComparer.Ordinal)
            .ToDictionary(
                name => name,
                name => new AuthorDao { Id = Guid.CreateVersion7(), Name = name },
                StringComparer.Ordinal);

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
                    CoverArtUrl = seed.CoverArtUrl,
                    Language = seed.Language,
                    PublicationState = seed.PublicationState,
                    Authors = [.. seed.Authors.Select(name => authorsByName[name])],
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

    private static IReadOnlyList<SeedBook> Read()
    {
        using var stream =
            Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException(
                $"The seed dataset '{ResourceName}' is not embedded in the assembly.");

        return JsonSerializer.Deserialize<List<SeedBook>>(stream, SerializerOptions)
            ?? throw new InvalidOperationException("The seed dataset is empty or malformed.");
    }

    private sealed record SeedBook(
        string Slug,
        string Title,
        string? Description,
        string? CoverArtUrl,
        string Language,
        Model.Book.PublicationState PublicationState,
        IReadOnlyList<string> Authors,
        IReadOnlyList<SeedChapter> Chapters);

    private sealed record SeedChapter(int Position, string Title, int RunningTimeSeconds);
}
