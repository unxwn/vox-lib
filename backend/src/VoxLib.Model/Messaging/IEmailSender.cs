namespace VoxLib.Model.Messaging;

/// <summary>
/// Delivers one message. Implemented in VoxLib.Platform over plain SMTP, so the
/// mail provider stays a configuration value rather than a code decision.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}

/// <summary>
/// One message. Plain text as well as HTML, because a mail client that shows
/// only the plain part must still carry a usable link.
/// </summary>
public sealed record EmailMessage(string To, string Subject, string PlainTextBody, string HtmlBody);
