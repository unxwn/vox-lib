using Microsoft.AspNetCore.Antiforgery;
using VoxLib.Api.Account.Contracts.Requests;
using VoxLib.Api.Account.Contracts.Responses;
using VoxLib.Model.Account;
using VoxLib.Orchestrator.Account;

namespace VoxLib.Api.Account;

/// <summary>
/// Registering, confirming an address and recovering a password. The handlers
/// parse the request, hand it to the rules and shape the answer. Which of the
/// outcomes applies is decided in VoxLib.Orchestrator, never here.
/// </summary>
public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder routes)
    {
        var account = routes.MapGroup("/api/account");

        account
            .MapGet(
                "/antiforgery-token",
                (IAntiforgery antiforgery, HttpContext context) =>
                {
                    var tokens = antiforgery.GetAndStoreTokens(context);

                    // Readable by the page, unlike the paired cookie the call
                    // above set. The page echoes it in a header, which another
                    // site cannot do because it cannot read this cookie.
                    context.Response.Cookies.Append(
                        AccountDefaults.AntiforgeryTokenCookieName,
                        tokens.RequestToken!,
                        new CookieOptions
                        {
                            HttpOnly = false,
                            SameSite = SameSiteMode.Strict,
                            Secure = context.Request.IsHttps,
                            Path = "/",
                        });

                    return Results.NoContent();
                })
            .WithName("GetAntiforgeryToken")
            .WithSummary("Obtain the antiforgery token for this browser")
            // Anonymous, unlike the sample in the framework documentation.
            // Registering and signing in change state and are made by someone
            // with no session, and signing in is itself a target of the attack
            // this guards against.
            .AllowAnonymous();

        account
            .MapGet(
                "/password-policy",
                () => Results.Ok(
                    new PasswordPolicyResponse(
                        PasswordPolicy.MinimumLength,
                        PasswordPolicy.RequiresDigit,
                        PasswordPolicy.RequiresUppercase,
                        PasswordPolicy.RequiresLowercase,
                        PasswordPolicy.RequiresNonAlphanumeric)))
            .WithName("GetPasswordPolicy")
            .WithSummary("What a password must satisfy")
            .Produces<PasswordPolicyResponse>()
            .AllowAnonymous();

        account
            .MapPost(
                "/registrations",
                async (
                    RegistrationRequest request,
                    Registration registration,
                    CancellationToken cancellationToken) =>
                {
                    var errors = AccountValidation.EmailAndPassword(
                        request.Email,
                        request.Password);

                    if (errors.Count > 0)
                    {
                        return AccountValidation.Problem(errors);
                    }

                    var outcome = await registration.RegisterAsync(
                        request.Email!,
                        request.Password!,
                        cancellationToken);

                    // There is one success and it is the same one whether the
                    // address was new or already had an account. The handler
                    // cannot tell the two apart either, which is the point.
                    return outcome == RegistrationOutcome.PasswordRejected
                        ? AccountValidation.Problem(
                            new Dictionary<string, string[]>
                            {
                                [AccountValidation.PasswordField] =
                                [
                                    AccountValidation.PasswordTooShort,
                                ],
                            })
                        : Results.Accepted();
                })
            .AddEndpointFilter<AntiforgeryGuard>()
            .RequireRateLimiting(AccountDefaults.ByClientPolicy)
            .WithName("Register")
            .WithSummary("Create an account")
            .Produces(StatusCodes.Status202Accepted)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .AllowAnonymous();

        account
            .MapPost(
                "/confirmations",
                async (
                    TokenRequest request,
                    EmailConfirmation confirmation,
                    CancellationToken cancellationToken) =>
                {
                    if (request.AccountId == Guid.Empty
                        || string.IsNullOrWhiteSpace(request.Token))
                    {
                        return Results.Problem(
                            title: AccountProblems.LinkExpired().Title,
                            detail: AccountProblems.LinkExpired().Detail,
                            statusCode: StatusCodes.Status410Gone);
                    }

                    var outcome = await confirmation.ConfirmAsync(
                        request.AccountId,
                        request.Token,
                        cancellationToken);

                    return outcome switch
                    {
                        ConfirmationOutcome.Confirmed =>
                            Results.Ok(ConfirmationResult.Confirmed),
                        ConfirmationOutcome.AlreadyConfirmed =>
                            Results.Ok(ConfirmationResult.AlreadyConfirmed),
                        _ => Results.Problem(
                            title: AccountProblems.LinkExpired().Title,
                            detail: AccountProblems.LinkExpired().Detail,
                            statusCode: StatusCodes.Status410Gone),
                    };
                })
            .AddEndpointFilter<AntiforgeryGuard>()
            .RequireRateLimiting(AccountDefaults.ByClientPolicy)
            .WithName("ConfirmEmail")
            .WithSummary("Confirm an address by redeeming the link sent to it")
            .Produces<ConfirmationResult>()
            .ProducesProblem(StatusCodes.Status410Gone)
            .AllowAnonymous();

        account
            .MapPost(
                "/confirmation-requests",
                async (
                    EmailOnlyRequest request,
                    EmailConfirmation confirmation,
                    CancellationToken cancellationToken) =>
                {
                    var errors = AccountValidation.Email(request.Email);

                    if (errors.Count > 0)
                    {
                        return AccountValidation.Problem(errors);
                    }

                    await confirmation.RequestAsync(request.Email!, cancellationToken);

                    // Always accepted. Whether a message went out is exactly the
                    // fact FR-005 forbids disclosing, so the answer cannot
                    // depend on it.
                    return Results.Accepted();
                })
            .AddEndpointFilter<AntiforgeryGuard>()
            .RequireRateLimiting(AccountDefaults.ByClientPolicy)
            .WithName("RequestConfirmation")
            .WithSummary("Ask for another confirmation message")
            .Produces(StatusCodes.Status202Accepted)
            .ProducesValidationProblem()
            .AllowAnonymous();

        account
            .MapPost(
                "/recovery-requests",
                async (
                    EmailOnlyRequest request,
                    PasswordRecovery recovery,
                    CancellationToken cancellationToken) =>
                {
                    var errors = AccountValidation.Email(request.Email);

                    if (errors.Count > 0)
                    {
                        return AccountValidation.Problem(errors);
                    }

                    await recovery.RequestAsync(request.Email!, cancellationToken);

                    // Always accepted, whether or not the address has an
                    // account. FR-005.
                    return Results.Accepted();
                })
            .AddEndpointFilter<AntiforgeryGuard>()
            .RequireRateLimiting(AccountDefaults.ByClientPolicy)
            .WithName("RequestRecovery")
            .WithSummary("Ask for a link that allows setting a new password")
            .Produces(StatusCodes.Status202Accepted)
            .ProducesValidationProblem()
            .AllowAnonymous();

        account
            .MapPost(
                "/recoveries",
                async (
                    PasswordResetRequest request,
                    PasswordRecovery recovery,
                    CancellationToken cancellationToken) =>
                {
                    if (string.IsNullOrEmpty(request.Password))
                    {
                        return AccountValidation.Problem(
                            new Dictionary<string, string[]>
                            {
                                [AccountValidation.PasswordField] = ["Вкажіть пароль."],
                            });
                    }

                    if (request.AccountId == Guid.Empty
                        || string.IsNullOrWhiteSpace(request.Token))
                    {
                        return Expired();
                    }

                    var outcome = await recovery.SetPasswordAsync(
                        request.AccountId,
                        request.Token,
                        request.Password,
                        cancellationToken);

                    return outcome switch
                    {
                        RecoveryOutcome.PasswordSet => Results.NoContent(),

                        RecoveryOutcome.PasswordRejected => AccountValidation.Problem(
                            new Dictionary<string, string[]>
                            {
                                [AccountValidation.PasswordField] =
                                [
                                    AccountValidation.PasswordTooShort,
                                ],
                            }),

                        _ => Expired(),
                    };
                })
            .AddEndpointFilter<AntiforgeryGuard>()
            .RequireRateLimiting(AccountDefaults.ByClientPolicy)
            .WithName("RecoverPassword")
            .WithSummary("Set a new password by redeeming a recovery link")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status410Gone)
            .AllowAnonymous();

        return routes;
    }

    private static IResult Expired()
    {
        var problem = AccountProblems.LinkExpired();

        return Results.Problem(
            title: problem.Title,
            detail: problem.Detail,
            type: problem.Type,
            statusCode: StatusCodes.Status410Gone);
    }
}
