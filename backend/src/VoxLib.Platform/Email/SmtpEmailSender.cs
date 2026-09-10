using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using VoxLib.Model.Messaging;

namespace VoxLib.Platform.Email;

/// <summary>
/// Delivers a message over SMTP. The library is MailKit because the framework's
/// own SmtpClient has been obsolete since .NET 5 and its documentation points
/// here by name.
/// <para>
/// Delivery is outside this system's control, which is why every flow in the
/// accounts feature that waits on a message also offers a way to ask for
/// another one.
/// </para>
/// </summary>
public sealed class SmtpEmailSender(IOptions<SmtpOptions> options) : IEmailSender
{
    private readonly SmtpOptions _options = options.Value;

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var mail = new MimeMessage
        {
            Subject = message.Subject,
            Body = new MultipartAlternative
            {
                new TextPart(TextFormat.Plain) { Text = message.PlainTextBody },
                new TextPart(TextFormat.Html) { Text = message.HtmlBody },
            },
        };

        mail.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        mail.To.Add(MailboxAddress.Parse(message.To));

        using var client = new SmtpClient();

        await client.ConnectAsync(
            _options.Host,
            _options.Port,
            _options.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None,
            cancellationToken);

        if (_options.User is { Length: > 0 })
        {
            await client.AuthenticateAsync(_options.User, _options.Password, cancellationToken);
        }

        await client.SendAsync(mail, cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);
    }
}
