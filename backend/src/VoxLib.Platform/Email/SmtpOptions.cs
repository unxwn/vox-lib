namespace VoxLib.Platform.Email;

/// <summary>
/// Where mail goes. Plain SMTP and nothing else: Resend, Brevo, Postmark and
/// Amazon SES all speak it, so choosing between them is a change to
/// configuration rather than to code.
/// </summary>
public sealed class SmtpOptions
{
    public const string Section = "Smtp";

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    /// <summary>
    /// False for the development mailbox, which speaks plain SMTP on the
    /// container's own network stack and never reaches a real address.
    /// </summary>
    public bool UseStartTls { get; set; } = true;

    public string User { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FromAddress { get; set; } = string.Empty;

    public string FromName { get; set; } = string.Empty;
}
