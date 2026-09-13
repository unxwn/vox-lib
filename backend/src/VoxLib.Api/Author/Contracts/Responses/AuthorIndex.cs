namespace VoxLib.Api.Author.Contracts.Responses;

/// <summary>
/// The author index. An object with one array rather than a bare array, so the
/// response has somewhere to grow — paging, a total — without becoming a
/// different shape.
/// </summary>
public sealed record AuthorIndex(IReadOnlyList<AuthorSummary> Items);
