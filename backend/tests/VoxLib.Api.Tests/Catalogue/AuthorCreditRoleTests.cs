using Microsoft.Extensions.DependencyInjection;
using VoxLib.Api.Tests.Infrastructure;
using VoxLib.Dal.Persistence;
using VoxLib.Model.Book;

namespace VoxLib.Api.Tests.Catalogue;

/// <summary>
/// FR-066: how someone is credited belongs to the link between a person and a
/// book, not to the person.
/// <para>
/// The proof is that one person credited two different ways stays one person.
/// Had the role been a column on the author, this would need two rows for them
/// and so two addresses; had it been a column on the book, the second book
/// could not disagree with the first. The wire half of this — the role beside
/// the name, and the author's own page — arrives with the author resource in
/// US4 and is asserted there.
/// </para>
/// </summary>
public class AuthorCreditRoleTests : IClassFixture<TwoRoleCatalogueApiFixture>
{
    private readonly TwoRoleCatalogueApiFixture _fixture;

    public AuthorCreditRoleTests(TwoRoleCatalogueApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task One_person_credited_two_ways_is_still_one_person()
    {
        var credits = await CreditsForCompilerAsync();

        Assert.Equal(2, credits.Count);
        Assert.Single(credits.Select(credit => credit.AuthorId).Distinct());
    }

    [Fact]
    public async Task The_role_differs_per_book_for_the_same_person()
    {
        var credits = await CreditsForCompilerAsync();

        var compiled = credits.Single(credit => credit.Role == CreditRole.Compiler);
        var authored = credits.Single(credit => credit.Role == CreditRole.Author);

        Assert.NotEqual(compiled.BookId, authored.BookId);
    }

    private async Task<List<BookAuthorRow>> CreditsForCompilerAsync()
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<VoxLibDbContext>();

        var person = database.Authors.Single(author =>
            author.Slug == SeededCatalogue.CompilerSlug);

        return
        [
            .. database
                .Set<VoxLib.Dal.Book.BookAuthorDao>()
                .Where(credit => credit.AuthorId == person.Id)
                .Select(credit => new BookAuthorRow(credit.AuthorId, credit.BookId, credit.Role)),
        ];
    }

    private sealed record BookAuthorRow(Guid AuthorId, Guid BookId, CreditRole Role);
}
