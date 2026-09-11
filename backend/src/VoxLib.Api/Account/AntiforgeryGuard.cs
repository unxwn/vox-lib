using Microsoft.AspNetCore.Antiforgery;

namespace VoxLib.Api.Account;

/// <summary>
/// Checks the antiforgery token on a request that changes state.
/// <para>
/// It is explicit rather than middleware because of what
/// <c>UseAntiforgery</c> does on .NET 10: it validates only endpoints that read
/// form data, and every endpoint here binds JSON, so the middleware would let
/// all of them through. .NET 11 adds a middleware that judges requests by their
/// fetch metadata instead; when this project moves, this can go.
/// </para>
/// </summary>
public sealed class AntiforgeryGuard(IAntiforgery antiforgery) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(context.HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            return Results.Problem(
                title: AccountProblems.AntiforgeryFailed().Title,
                detail: AccountProblems.AntiforgeryFailed().Detail,
                statusCode: StatusCodes.Status400BadRequest);
        }

        return await next(context);
    }
}
