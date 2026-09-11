using Microsoft.AspNetCore.Mvc;

namespace VoxLib.Api.Account;

/// <summary>
/// The refusals this feature can give, in Ukrainian, because a person reads
/// them.
/// <para>
/// They are built here, in one place, because the wording is load-bearing.
/// FR-015 requires an unknown address and a wrong password to be answered
/// identically, and FR-005 requires that nothing distinguishes an address that
/// has an account from one that does not. Two handlers writing their own text
/// is how that stops being true.
/// </para>
/// </summary>
public static class AccountProblems
{
    /// <summary>
    /// One answer for an unknown address and for a known address with the wrong
    /// password. FR-015.
    /// </summary>
    public static ProblemDetails CredentialsNotRecognised() =>
        new()
        {
            Type = "https://vox-lib/problems/credentials-not-recognised",
            Title = "Не вдалося увійти",
            Detail = "Такого поєднання адреси й пароля не знайдено.",
            Status = StatusCodes.Status401Unauthorized,
        };

    /// <summary>
    /// The one response that admits an account exists, reachable only by
    /// supplying that account's password. FR-011.
    /// </summary>
    public static ProblemDetails EmailNotConfirmed() =>
        new()
        {
            Type = "https://vox-lib/problems/email-not-confirmed",
            Title = "Адресу не підтверджено",
            Detail =
                "Спочатку підтвердьте адресу електронної пошти. "
                    + "Ми можемо надіслати новий лист із посиланням.",
            Status = StatusCodes.Status403Forbidden,
        };

    /// <summary>
    /// Given both for a lockout and for too many attempts against an address
    /// that has no account, so the two cannot be told apart. This is what lets
    /// FR-016 tell a person what happened without breaking FR-005.
    /// </summary>
    public static ProblemDetails TooManyAttempts(TimeSpan retryAfter) =>
        new()
        {
            Type = "https://vox-lib/problems/too-many-attempts",
            Title = "Забагато спроб",
            Detail =
                "Спроб було забагато. Спробуйте ще раз через "
                    + $"{Math.Max(1, (int)Math.Ceiling(retryAfter.TotalMinutes))} хв.",
            Status = StatusCodes.Status429TooManyRequests,
        };

    /// <summary>
    /// A link that has been used or has passed its lifetime. Both are offered a
    /// replacement. FR-008 and FR-024.
    /// </summary>
    public static ProblemDetails LinkExpired() =>
        new()
        {
            Type = "https://vox-lib/problems/link-expired",
            Title = "Посилання більше не дійсне",
            Detail =
                "Термін дії посилання минув або його вже було використано. "
                    + "Надішліть новий лист із посиланням.",
            Status = StatusCodes.Status410Gone,
        };

    public static ProblemDetails AntiforgeryFailed() =>
        new()
        {
            Type = "https://vox-lib/problems/antiforgery-failed",
            Title = "Запит відхилено",
            Detail = "Не вдалося підтвердити, що запит надійшов із цього сайту. Оновіть сторінку.",
            Status = StatusCodes.Status400BadRequest,
        };
}
