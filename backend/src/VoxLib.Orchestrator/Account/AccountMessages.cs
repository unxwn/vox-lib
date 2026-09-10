using VoxLib.Model.Account;
using VoxLib.Model.Messaging;

namespace VoxLib.Orchestrator.Account;

/// <summary>
/// What this feature says, in Ukrainian, and to whom. Delivery belongs to
/// VoxLib.Platform; the wording belongs here, because two of these messages are
/// load-bearing for FR-005.
/// <para>
/// Every message carries the link in the plain text part as well as the HTML
/// one. A mail client that shows only plain text still has to give a person a
/// usable link, and for someone reading with a screen reader that is often the
/// part they are given.
/// </para>
/// </summary>
public sealed class AccountMessages(IEmailSender sender, SiteAddress site) : IAccountMessages
{
    public Task SendConfirmationAsync(
        Guid accountId,
        string email,
        string token,
        CancellationToken cancellationToken)
    {
        var link = site.Link("confirm", accountId, token);

        return sender.SendAsync(
            new EmailMessage(
                email,
                "Підтвердьте адресу для vox-lib",
                $"""
                Вітаємо!

                Щоб користуватися обліковим записом vox-lib, підтвердьте цю адресу
                електронної пошти. Перейдіть за посиланням:

                {link}

                Посилання дійсне 24 години. Якщо ви не створювали облікового запису,
                просто проігноруйте цей лист.
                """,
                $"""
                <p>Вітаємо!</p>
                <p>
                  Щоб користуватися обліковим записом vox-lib, підтвердьте цю адресу
                  електронної пошти.
                </p>
                <p><a href="{link}">Підтвердити адресу</a></p>
                <p>
                  Посилання дійсне 24 години. Якщо ви не створювали облікового запису,
                  просто проігноруйте цей лист.
                </p>
                """),
            cancellationToken);
    }

    /// <summary>
    /// Sent instead of a confirmation when the address already has an account.
    /// <para>
    /// This is what lets registration answer identically whether or not the
    /// address is known. The response cannot say "you already have an account",
    /// because anybody could ask; a message to the address can, because only the
    /// person who owns it will read it. It carries no link and creates nothing.
    /// </para>
    /// </summary>
    public Task SendAlreadyRegisteredAsync(string email, CancellationToken cancellationToken)
    {
        var signIn = $"{site.BaseAddress.TrimEnd('/')}/sign-in";
        var forgot = $"{site.BaseAddress.TrimEnd('/')}/forgot-password";

        return sender.SendAsync(
            new EmailMessage(
                email,
                "Обліковий запис vox-lib для цієї адреси вже існує",
                $"""
                Вітаємо!

                Хтось намагався створити обліковий запис vox-lib із цією адресою, але
                вона вже використовується. Нового запису не створено.

                Якщо це були ви, увійдіть: {signIn}
                Якщо ви забули пароль, відновіть доступ: {forgot}

                Якщо це були не ви, нічого робити не потрібно.
                """,
                $"""
                <p>Вітаємо!</p>
                <p>
                  Хтось намагався створити обліковий запис vox-lib із цією адресою, але
                  вона вже використовується. Нового запису не створено.
                </p>
                <p>Якщо це були ви, <a href="{signIn}">увійдіть</a>.</p>
                <p>Якщо ви забули пароль, <a href="{forgot}">відновіть доступ</a>.</p>
                <p>Якщо це були не ви, нічого робити не потрібно.</p>
                """),
            cancellationToken);
    }

    public Task SendRecoveryAsync(
        Guid accountId,
        string email,
        string token,
        CancellationToken cancellationToken)
    {
        var link = site.Link("reset-password", accountId, token);

        return sender.SendAsync(
            new EmailMessage(
                email,
                "Відновлення пароля vox-lib",
                $"""
                Вітаємо!

                Ви попросили встановити новий пароль для vox-lib. Перейдіть за
                посиланням:

                {link}

                Посилання дійсне 24 години й спрацює один раз. Щойно ви встановите
                новий пароль, усі сеанси, відкриті раніше, буде завершено.

                Якщо ви цього не просили, просто проігноруйте цей лист: пароль
                залишиться попереднім.
                """,
                $"""
                <p>Вітаємо!</p>
                <p>Ви попросили встановити новий пароль для vox-lib.</p>
                <p><a href="{link}">Встановити новий пароль</a></p>
                <p>
                  Посилання дійсне 24 години й спрацює один раз. Щойно ви встановите
                  новий пароль, усі сеанси, відкриті раніше, буде завершено.
                </p>
                <p>
                  Якщо ви цього не просили, просто проігноруйте цей лист: пароль
                  залишиться попереднім.
                </p>
                """),
            cancellationToken);
    }
}
