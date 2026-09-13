using VoxLib.Api.Search.Contracts.Responses;
using VoxLib.Model.Book;
using ResponseSuggestion = VoxLib.Api.Search.Contracts.Responses.SearchSuggestion;

namespace VoxLib.Api.Search;

/// <summary>
/// The suggestions that make the search field say what it is for by example.
/// </summary>
public static class SearchEndpoints
{
    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder routes)
    {
        routes
            .MapGet(
                "/api/search-suggestions",
                async (
                    ICatalogueSuggestions suggestions,
                    HttpResponse response,
                    CancellationToken cancellationToken) =>
                {
                    var sample = await suggestions.SampleAsync(cancellationToken);

                    // Load-bearing rather than tidy. The sample is drawn fresh
                    // per request, so a cached response would freeze one
                    // visitor's handful forever while staying fresh on the
                    // server — the field would then show the same two examples
                    // on every visit and the suggestions beneath it would never
                    // change, which is the entire point of sampling.
                    response.Headers.CacheControl = "no-store";

                    return Results.Ok(
                        new SearchSuggestions(
                            [
                                .. sample.Select(one =>
                                    new ResponseSuggestion(one.Term, one.Kind)),
                            ]));
                })
            .WithName("ListSearchSuggestions")
            .WithSummary("A small random sample of things worth searching for")
            .Produces<SearchSuggestions>();

        return routes;
    }
}
