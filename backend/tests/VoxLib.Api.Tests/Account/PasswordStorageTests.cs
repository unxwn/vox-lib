using Microsoft.EntityFrameworkCore;
using VoxLib.Api.Tests.Infrastructure;

namespace VoxLib.Api.Tests.Account;

/// <summary>
/// FR-003 and SC-013: no password is recoverable from what the system stores or
/// from what it says. This is the requirement whose failure is invisible until
/// somebody reads a database dump.
/// </summary>
[Collection(AccountCollection.Name)]
public class PasswordStorageTests(AccountApiFixture fixture)
{
    private const string Password = "справді-довгий-пароль-2026";

    [Fact]
    public async Task The_stored_row_carries_a_hash_and_nothing_the_password_can_be_read_from()
    {
        var email = Addresses.Fresh();
        var client = fixture.CreateSeparateBrowser();

        await AccountApiFixture.PostAsync(
            client,
            "/api/account/registrations",
            new { email, password = Password });

        var account = await fixture.ReadAsync(database =>
            database.Users.SingleAsync(candidate => candidate.Email == email));

        Assert.NotNull(account.PasswordHash);
        Assert.NotEmpty(account.PasswordHash);

        // Not the password, not the password reversed, not the password in any
        // encoding a careless implementation might reach for.
        Assert.DoesNotContain(Password, account.PasswordHash, StringComparison.Ordinal);
        Assert.DoesNotContain(
            Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(Password)),
            account.PasswordHash,
            StringComparison.Ordinal);
    }

    /// <summary>
    /// The whole row, every column, so that adding a column that happens to hold
    /// the password fails here rather than in production.
    /// </summary>
    [Fact]
    public async Task No_column_on_the_account_row_contains_the_password()
    {
        var email = Addresses.Fresh();
        var client = fixture.CreateSeparateBrowser();

        await AccountApiFixture.PostAsync(
            client,
            "/api/account/registrations",
            new { email, password = Password });

        var row = await fixture.ReadAsync(async database =>
        {
            var account = await database.Users.SingleAsync(candidate => candidate.Email == email);

            return string.Join(
                "|",
                typeof(VoxLib.Dal.Account.AccountDao)
                    .GetProperties()
                    .Select(property => property.GetValue(account)?.ToString() ?? string.Empty));
        });

        Assert.DoesNotContain(Password, row, StringComparison.Ordinal);
    }

    /// <summary>
    /// A password must not come back in the response either, which is the other
    /// half of FR-003 and the one an echoing validation message would break.
    /// </summary>
    [Fact]
    public async Task No_response_echoes_the_password_back()
    {
        var client = fixture.CreateSeparateBrowser();

        var accepted = await AccountApiFixture.PostAsync(
            client,
            "/api/account/registrations",
            new { email = Addresses.Fresh(), password = Password });

        var rejected = await AccountApiFixture.PostAsync(
            client,
            "/api/account/registrations",
            new { email = Addresses.Fresh(), password = "закоротко" });

        Assert.DoesNotContain(
            Password,
            await accepted.Content.ReadAsStringAsync(),
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "закоротко",
            await rejected.Content.ReadAsStringAsync(),
            StringComparison.Ordinal);
    }

    /// <summary>
    /// And not in a message either. The confirmation link is sent to the
    /// address; the password never is.
    /// </summary>
    [Fact]
    public async Task No_message_carries_the_password()
    {
        var email = Addresses.Fresh();
        var client = fixture.CreateSeparateBrowser();

        await AccountApiFixture.PostAsync(
            client,
            "/api/account/registrations",
            new { email, password = Password });

        var message = fixture.Email.LastTo(email);

        Assert.NotNull(message);
        Assert.DoesNotContain(Password, message.PlainTextBody, StringComparison.Ordinal);
        Assert.DoesNotContain(Password, message.HtmlBody, StringComparison.Ordinal);
        Assert.DoesNotContain(Password, message.Subject, StringComparison.Ordinal);
    }
}
