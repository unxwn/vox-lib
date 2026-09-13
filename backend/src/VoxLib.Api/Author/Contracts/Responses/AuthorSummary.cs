using VoxLib.Model.Book;

namespace VoxLib.Api.Author.Contracts.Responses;

/// <summary>
/// One person as the index lists them.
/// <para>
/// <see cref="Roles"/> is a set rather than the single role an
/// <see cref="AuthorReference"/> carries, and the difference is the whole reason
/// the two shapes are separate. A reference describes one credit on one book; an
/// index entry describes a person across all of theirs, and one person may be an
/// author on one and a compiler on another. The index cannot claim a single role
/// for them without being wrong (FR-035).
/// </para>
/// </summary>
/// <param name="SortName">Прізвище, Ім'я. What the index is ordered by.</param>
/// <param name="BookCount">
/// Published books only, so it never promises a book a visitor cannot see.
/// </param>
public sealed record AuthorSummary(
    string Slug,
    string Name,
    string SortName,
    int BookCount,
    IReadOnlyList<CreditRole> Roles);
