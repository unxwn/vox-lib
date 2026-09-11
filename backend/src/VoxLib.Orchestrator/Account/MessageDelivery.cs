using Microsoft.Extensions.Logging;

namespace VoxLib.Orchestrator.Account;

/// <summary>
/// Sending a message without letting a failure change the answer.
/// <para>
/// This exists because of a leak, not because of tidiness. A message goes out
/// only for an address that has an account, so if a failure to send reached the
/// caller, a mail outage would answer one way for an address that is registered
/// and another way for one that is not. That is precisely the disclosure FR-005
/// forbids, and it would appear the moment the mail provider had a bad day
/// rather than in any test written against a working one.
/// </para>
/// <para>
/// It also protects work already done. Registering creates the account before it
/// sends anything, so a failure that propagated would leave the person with an
/// account they cannot confirm and an error that suggests they have none. Every
/// flow here offers a way to ask for the message again, which is what makes
/// swallowing it recoverable rather than merely quiet.
/// </para>
/// </summary>
internal static class MessageDelivery
{
    public static async Task TryAsync(
        Func<Task> send,
        ILogger logger,
        string what)
    {
        try
        {
            await send();
        }
        catch (Exception exception)
        {
            // Deliberately no address in the message. A log is a place a
            // password or an address should never end up, and FR-003 is about
            // logs as much as about storage.
            logger.LogError(
                exception,
                "Could not deliver a {MessageKind} message. The account flow continued, and the "
                    + "person can ask for the message again.",
                what);
        }
    }
}
