using VoxLib.Model.Book;

namespace VoxLib.Api.Author.Contracts.Responses;

/// <summary>
/// A person as credited on one particular book.
/// <para>
/// The role is here and not on <see cref="AuthorIdentity"/> because it is a fact
/// about the pairing: the same person can be an author on one book and a
/// compiler on another, and an identity claiming one role would be wrong about
/// the other (FR-066).
/// </para>
/// </summary>
public sealed record AuthorReference(string Slug, string Name, CreditRole Role);
