using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using VoxLib.Model.Messaging;

namespace VoxLib.Api.Tests.Infrastructure;

/// <summary>
/// Keeps what the application asked to send, so a test can take the link out of
/// a message and follow it, which is the journey a person makes.
/// <para>
/// This is the one boundary the account tests substitute. Everything else runs
/// for real: the hashing, the security stamp, the cookie and the token. Sending
/// mail for real from a test run is the alternative, and it would make every
/// test that involves a link depend on delivery this system does not control.
/// </para>
/// </summary>
public sealed partial class RecordingEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<EmailMessage> _sent = new();

    public IReadOnlyCollection<EmailMessage> Sent => _sent;

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        _sent.Enqueue(message);
        return Task.CompletedTask;
    }

    /// <summary>The most recent message sent to this address, or null.</summary>
    public EmailMessage? LastTo(string email) =>
        _sent
            .Where(message => string.Equals(message.To, email, StringComparison.OrdinalIgnoreCase))
            .LastOrDefault();

    public int CountTo(string email) =>
        _sent.Count(message =>
            string.Equals(message.To, email, StringComparison.OrdinalIgnoreCase));

    public void Clear() => _sent.Clear();

    /// <summary>
    /// The account id and token carried by the most recent message to this
    /// address. Read out of the plain text part, because a mail client that
    /// shows only that part still has to give a person a usable link.
    /// </summary>
    public (Guid AccountId, string Token) LinkTo(string email)
    {
        var message = LastTo(email)
            ?? throw new InvalidOperationException($"No message was sent to {email}.");

        var match = LinkPattern().Match(message.PlainTextBody);

        if (!match.Success)
        {
            throw new InvalidOperationException(
                $"No link with an account id and a token was found in the message to {email}:"
                    + Environment.NewLine
                    + message.PlainTextBody);
        }

        return (Guid.Parse(match.Groups["id"].Value), Uri.UnescapeDataString(match.Groups["token"].Value));
    }

    [GeneratedRegex(@"[?&]account=(?<id>[0-9a-fA-F-]{36})&token=(?<token>[^\s&]+)")]
    private static partial Regex LinkPattern();
}
