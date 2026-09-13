namespace VoxLib.Api.Author.Contracts.Responses;

/// <summary>
/// Who someone is, with nothing about any particular book.
/// <para>
/// It carries the slug, which is what lets a credited name be a link without a
/// second request for the address (FR-033). Before this, a book's authors were
/// bare strings and the frontend had no way to reach the person.
/// </para>
/// </summary>
public sealed record AuthorIdentity(string Slug, string Name);
