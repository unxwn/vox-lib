using VoxLib.Api.Author.Contracts.Responses;
using VoxLib.Api.Book;
using VoxLib.Model.Book;
using VoxLib.Model.Storage;

namespace VoxLib.Api.Author;

/// <summary>
/// Authors as a resource of their own, which is what FR-036 asks for and what
/// Principle V agrees with: a person is a thing the catalogue holds, not a
/// filter over books.
/// </summary>
public static class AuthorEndpoints
{
    public static IEndpointRouteBuilder MapAuthorEndpoints(this IEndpointRouteBuilder routes)
    {
        routes
            .MapGet(
                "/api/authors",
                async (IAuthorCatalogue authors, CancellationToken cancellationToken) =>
                {
                    var found = await authors.ListAsync(cancellationToken);

                    return Results.Ok(new AuthorIndex([.. found.Select(ToSummary)]));
                })
            .WithName("ListAuthors")
            .WithSummary("Every author credited on a published book, ordered by sort name")
            .Produces<AuthorIndex>();

        routes
            .MapGet(
                "/api/authors/{slug}",
                async (
                    string slug,
                    IAuthorCatalogue authors,
                    ICoverStorage covers,
                    CancellationToken cancellationToken) =>
                {
                    var found = await authors.FindAsync(slug, cancellationToken);

                    // One answer for an author who was never here and for one
                    // whose only books are drafts. The handler cannot tell them
                    // apart either, which is the point.
                    return found is null
                        ? Results.Problem(
                            title: "No such author",
                            detail: $"The catalogue has no author '{slug}'.",
                            statusCode: StatusCodes.Status404NotFound)
                        : Results.Ok(ToDetail(found, covers));
                })
            .WithName("GetAuthor")
            .WithSummary("One author and every published book they are credited on")
            .Produces<AuthorDetail>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return routes;
    }

    private static AuthorSummary ToSummary(AuthorWithBooks found) =>
        new(
            found.Author.Slug,
            found.Author.Name,
            found.Author.SortName,
            found.Books.Count,
            found.Roles);

    private static AuthorDetail ToDetail(AuthorWithBooks found, ICoverStorage covers) =>
        new(
            found.Author.Slug,
            found.Author.Name,
            found.Author.SortName,
            found.Books.Count,
            found.Roles,
            [.. found.Books.Select(book => BookEndpoints.ToSummary(book, covers))]);
}
