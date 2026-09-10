using VoxLib.Api.Account.Contracts.Requests;
using VoxLib.Api.Account.Contracts.Responses;
using VoxLib.Model.Account;
using VoxLib.Orchestrator.Account;

namespace VoxLib.Api.Account;

/// <summary>
/// The browser's session as a resource: POST creates it, GET reads it, DELETE
/// ends it.
/// <para>
/// The four refusals map to four status codes, and which one applies is decided
/// in the orchestrator. The handler's only judgement is that the 429 it produces
/// for a lockout is byte for byte the one the rate limiter produces for too many
/// attempts, because an address with no account has to be refused in exactly the
/// same way as one that exists.
/// </para>
/// </summary>
public static class SessionEndpoints
{
    public static IEndpointRouteBuilder MapSessionEndpoints(this IEndpointRouteBuilder routes)
    {
        var session = routes.MapGroup("/api/account/session");

        session
            .MapGet(
                "",
                (IAccountSession current) =>
                {
                    var listener = current.Current;

                    return Results.Ok(
                        listener.IsSignedIn
                            ? new SessionResponse(
                                true,
                                listener.Email,
                                listener.IsVerifiedBeneficiary)
                            : SessionResponse.Anonymous);
                })
            .WithName("GetSession")
            .WithSummary("Who this browser is signed in as")
            .Produces<SessionResponse>()
            .AllowAnonymous();

        session
            .MapPost(
                "",
                async (
                    SignInRequest request,
                    SignIn signIn,
                    HttpContext context,
                    CancellationToken cancellationToken) =>
                {
                    var errors = AccountValidation.Email(request.Email);

                    if (string.IsNullOrEmpty(request.Password))
                    {
                        errors[AccountValidation.PasswordField] = ["Вкажіть пароль."];
                    }

                    if (errors.Count > 0)
                    {
                        return AccountValidation.Problem(errors);
                    }

                    var result = await signIn.AttemptAsync(
                        request.Email!,
                        request.Password!,
                        cancellationToken);

                    return result.Outcome switch
                    {
                        SignInOutcome.Succeeded => Results.Ok(
                            new SessionResponse(
                                true,
                                result.Listener.Email,
                                result.Listener.IsVerifiedBeneficiary)),

                        SignInOutcome.EmailNotConfirmed => Problem(
                            AccountProblems.EmailNotConfirmed()),

                        SignInOutcome.LockedOut => TooManyAttempts(context),

                        // Unknown address and wrong password land here together,
                        // and there is deliberately nothing to tell them apart.
                        _ => Problem(AccountProblems.CredentialsNotRecognised()),
                    };
                })
            .AddEndpointFilter<AntiforgeryGuard>()
            .RequireRateLimiting(AccountDefaults.ByClientPolicy)
            .WithName("SignIn")
            .WithSummary("Sign in")
            .Produces<SessionResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .AllowAnonymous();

        session
            .MapDelete(
                "",
                async (SignIn signIn, CancellationToken cancellationToken) =>
                {
                    await signIn.SignOutAsync(cancellationToken);

                    // 204 whether or not there was a session, so signing out
                    // twice is not an error and a person on a shared device is
                    // never left wondering whether it worked.
                    return Results.NoContent();
                })
            .AddEndpointFilter<AntiforgeryGuard>()
            .WithName("SignOut")
            .WithSummary("Sign out")
            .Produces(StatusCodes.Status204NoContent)
            .AllowAnonymous();

        return routes;
    }

    private static IResult Problem(Microsoft.AspNetCore.Mvc.ProblemDetails problem) =>
        Results.Problem(
            title: problem.Title,
            detail: problem.Detail,
            type: problem.Type,
            statusCode: problem.Status);

    /// <summary>
    /// The refusal for too many attempts, whether the account was locked out or
    /// the address simply ran out of allowance.
    /// <para>
    /// The wait told to the person is the stated period rather than the exact
    /// time remaining, deliberately. An exact countdown differs between an
    /// account that locked out three minutes ago and an address that has none,
    /// and that difference is a way to find out which is which. FR-016 asks for
    /// a stated period, and this is it.
    /// </para>
    /// </summary>
    private static IResult TooManyAttempts(HttpContext context)
    {
        var problem = AccountProblems.TooManyAttempts(AccountDefaults.LockoutDuration);

        // FR-016: the person is told when they may try again. Written on the
        // response itself, because Results.Problem carries no headers and a
        // refusal without this is one the rate limiter would not have produced.
        context.Response.Headers.RetryAfter =
            ((int)AccountDefaults.LockoutDuration.TotalSeconds).ToString();

        return Results.Problem(
            title: problem.Title,
            detail: problem.Detail,
            type: problem.Type,
            statusCode: StatusCodes.Status429TooManyRequests);
    }
}
