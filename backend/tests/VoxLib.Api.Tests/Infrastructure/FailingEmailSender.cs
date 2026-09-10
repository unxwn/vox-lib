using VoxLib.Model.Messaging;

namespace VoxLib.Api.Tests.Infrastructure;

/// <summary>A mail provider that is having a bad day.</summary>
public sealed class FailingEmailSender : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("The mail provider is unavailable.");
}
