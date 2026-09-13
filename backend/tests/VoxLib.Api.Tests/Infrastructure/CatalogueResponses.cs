namespace VoxLib.Api.Tests.Infrastructure;

/// <summary>
/// The wire shapes the tests read, declared independently of the API's own
/// contracts so that a test fails when a field is renamed rather than quietly
/// following it. They mirror contracts/catalogue.yaml.
/// </summary>
public sealed record PagedBooksResponse(
    IReadOnlyList<BookSummaryResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int PageCount);

public sealed record BookSummaryResponse(
    string Slug,
    string Title,
    IReadOnlyList<AuthorReferenceResponse> Authors,
    CoverResponse? Cover,
    int ChapterCount,
    int TotalRunningTimeSeconds);

public sealed record BookDetailResponse(
    string Slug,
    string Title,
    IReadOnlyList<AuthorReferenceResponse> Authors,
    string? Description,
    CoverResponse? Cover,
    string? Narrator,
    string Language,
    int ChapterCount,
    int TotalRunningTimeSeconds,
    IReadOnlyList<ChapterResponse> Chapters);

/// <summary>
/// A cover as a set of prepared widths rather than the one URL version 1
/// carried. Null on a book with no cover, which is a designed state.
/// </summary>
public sealed record CoverResponse(IReadOnlyList<CoverSourceResponse> Sources);

public sealed record CoverSourceResponse(string Url, int Width);

public sealed record ChapterResponse(int Position, string Title, int RunningTimeSeconds);

/// <summary>The search suggestions, as contracts/catalogue.yaml declares them.</summary>
public sealed record SearchSuggestionsResponse(IReadOnlyList<SearchSuggestionResponse> Items);

public sealed record SearchSuggestionResponse(string Term, string Kind);

/// <summary>An author as the index lists them.</summary>
public sealed record AuthorSummaryResponse(
    string Slug,
    string Name,
    string SortName,
    int BookCount,
    IReadOnlyList<string> Roles);

public sealed record AuthorIndexResponse(IReadOnlyList<AuthorSummaryResponse> Items);

public sealed record AuthorDetailResponse(
    string Slug,
    string Name,
    string SortName,
    int BookCount,
    IReadOnlyList<string> Roles,
    IReadOnlyList<BookSummaryResponse> Books);

/// <summary>A credit on one book: who, and how they are credited.</summary>
public sealed record AuthorReferenceResponse(string Slug, string Name, string Role);
