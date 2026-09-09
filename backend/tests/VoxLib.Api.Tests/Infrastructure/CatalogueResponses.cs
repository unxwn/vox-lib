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
    IReadOnlyList<string> Authors,
    string? CoverArtUrl,
    int ChapterCount,
    int TotalRunningTimeSeconds);

public sealed record BookDetailResponse(
    string Slug,
    string Title,
    IReadOnlyList<string> Authors,
    string? Description,
    string? CoverArtUrl,
    string Language,
    int ChapterCount,
    int TotalRunningTimeSeconds,
    IReadOnlyList<ChapterResponse> Chapters);

public sealed record ChapterResponse(int Position, string Title, int RunningTimeSeconds);
