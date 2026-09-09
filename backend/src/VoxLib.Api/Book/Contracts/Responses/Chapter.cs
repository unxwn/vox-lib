namespace VoxLib.Api.Book.Contracts.Responses;

/// <summary>
/// One chapter of a book: where it falls, what it is called and how long it
/// runs. Nothing else, because there is nothing else a reader may have yet.
/// </summary>
/// <param name="Position">One-based, and contiguous within a book.</param>
public sealed record Chapter(int Position, string Title, int RunningTimeSeconds);
