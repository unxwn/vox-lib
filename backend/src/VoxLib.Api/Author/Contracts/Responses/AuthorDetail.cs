using VoxLib.Api.Book.Contracts.Responses;
using VoxLib.Model.Book;

namespace VoxLib.Api.Author.Contracts.Responses;

/// <summary>
/// One person and every published book they are credited on.
/// <para>
/// The role held on each book is read from that book's own authors array rather
/// than repeated here, so the two can never disagree about the same fact.
/// </para>
/// </summary>
public sealed record AuthorDetail(
    string Slug,
    string Name,
    string SortName,
    int BookCount,
    IReadOnlyList<CreditRole> Roles,
    IReadOnlyList<BookSummary> Books);
