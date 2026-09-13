using VoxLib.Model.Book;

namespace VoxLib.Orchestrator.Book;

/// <summary>
/// The rules about authors: who appears, in what order, and who is not found.
/// Endpoint handlers parse and shape; the decisions are here.
/// </summary>
public sealed class AuthorCatalogue(IAuthorRepository authors) : IAuthorCatalogue
{
    public Task<IReadOnlyList<AuthorWithBooks>> ListAsync(CancellationToken cancellationToken) =>
        authors.ListAsync(cancellationToken);

    public async Task<AuthorWithBooks?> FindAsync(
        string slug,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        var found = await authors.FindBySlugAsync(slug, cancellationToken);

        // Asked again on the way out. The repository already filters, so this is
        // belt and braces on the rule that matters: an author with nothing a
        // visitor may see is not found, exactly as an absent one is. A page
        // listing no books would be a dead end wearing a name (FR-037).
        return found is { Books.Count: > 0 } ? found : null;
    }
}
