using System.Net.Mail;
using VoxLib.Model.Account;

namespace VoxLib.Api.Account;

/// <summary>
/// Whether a submission is well formed, and if not, which field is wrong.
/// <para>
/// The shape is per field rather than one list, because FR-031 needs each
/// failure tied to the field it concerns so a screen reader user learns which
/// one to correct without hunting. The messages are Ukrainian because a person
/// reads them.
/// </para>
/// </summary>
public static class AccountValidation
{
    public const string EmailField = "email";

    public const string PasswordField = "password";

    public static Dictionary<string, string[]> Email(string? email)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(email))
        {
            errors[EmailField] = ["Вкажіть адресу електронної пошти."];
            return errors;
        }

        if (email.Length > AccountDefaults.MaxEmailLength)
        {
            errors[EmailField] =
            [
                $"Адреса задовга: не більше {AccountDefaults.MaxEmailLength} символів.",
            ];
            return errors;
        }

        if (!MailAddress.TryCreate(email, out _))
        {
            errors[EmailField] = ["Це не схоже на адресу електронної пошти."];
        }

        return errors;
    }

    public static Dictionary<string, string[]> EmailAndPassword(string? email, string? password)
    {
        var errors = Email(email);

        if (string.IsNullOrEmpty(password))
        {
            errors[PasswordField] = ["Вкажіть пароль."];
            return errors;
        }

        if (password.Length > PasswordPolicy.MaximumLength)
        {
            errors[PasswordField] =
            [
                $"Пароль задовгий: не більше {PasswordPolicy.MaximumLength} символів.",
            ];
            return errors;
        }

        if (!PasswordPolicy.IsLongEnough(password))
        {
            errors[PasswordField] = [PasswordTooShort];
        }

        return errors;
    }

    /// <summary>
    /// Named once, so the message the interface shows before submitting and the
    /// message it shows after a refusal say the same thing.
    /// </summary>
    public static string PasswordTooShort =>
        $"Пароль має містити щонайменше {PasswordPolicy.MinimumLength} символів.";

    public static IResult Problem(Dictionary<string, string[]> errors) =>
        Results.ValidationProblem(
            errors,
            title: "Перевірте введені дані",
            detail: "Деякі поля заповнено неправильно.");
}
