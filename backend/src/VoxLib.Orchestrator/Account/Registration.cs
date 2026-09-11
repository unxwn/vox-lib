using Microsoft.Extensions.Logging;
using VoxLib.Model.Account;

namespace VoxLib.Orchestrator.Account;

/// <summary>
/// Creating an account.
/// <para>
/// The decision this class exists to hold is that the caller is never told
/// whether the address was already taken. Both paths end in the same outcome;
/// only the message sent to the address differs. FR-005, and it is a rule rather
/// than a shape, so it lives here and not in the endpoint.
/// </para>
/// </summary>
public sealed class Registration(
    IAccountStore accounts,
    IAccountLinks links,
    IAccountMessages messages,
    IAttemptLimiter limiter,
    ILogger<Registration> logger)
{
    public async Task<RegistrationOutcome> RegisterAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        if (!PasswordPolicy.IsLongEnough(password))
        {
            return RegistrationOutcome.PasswordRejected;
        }

        // A limit per address, so that registering cannot be used to send mail
        // repeatedly to somebody who did not ask for it. Refusing here still
        // answers Accepted: saying "you are being rate limited on this address"
        // would itself be a signal about the address.
        if (!limiter.Consume(email, AttemptKind.SendMessage).Allowed)
        {
            return RegistrationOutcome.Accepted;
        }

        var account = await accounts.CreateAsync(email, password, cancellationToken);

        if (account is null)
        {
            // The address already has an account. The response will not say so,
            // but the person who owns the address is told, because only they
            // will read it.
            //
            // No extra work is needed to keep the timing alike: creating hashes
            // the password before it discovers the duplicate, so both paths have
            // already paid the expensive part.
            await MessageDelivery.TryAsync(
                () => messages.SendAlreadyRegisteredAsync(email, cancellationToken),
                logger,
                "already registered");

            return RegistrationOutcome.Accepted;
        }

        var token = await links.IssueAsync(
            account.Id,
            LinkPurpose.ConfirmEmail,
            cancellationToken);

        // A message that could not be sent must not undo the account or change
        // the answer. The person is told to check their inbox and is offered a
        // way to ask again, which is the only recovery available when delivery
        // is outside this system's control.
        await MessageDelivery.TryAsync(
            () => messages.SendConfirmationAsync(account.Id, email, token, cancellationToken),
            logger,
            "confirmation");

        return RegistrationOutcome.Accepted;
    }
}
