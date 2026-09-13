using System.Net.Http.Json;
using VoxLib.Api.Tests.Infrastructure;

namespace VoxLib.Api.Tests.Catalogue;

/// <summary>
/// SC-014 and SC-029: the index orders by surname under Ukrainian collation and
/// names how each person is credited.
/// </summary>
[Collection(CatalogueCollection.Name)]
public class AuthorIndexOrderTests(CatalogueApiFixture fixture)
{
    private readonly HttpClient _client = fixture.CreateClient();

    /// <summary>
    /// Against the expected Ukrainian order rather than against insertion order.
    /// The distinction matters: the seed inserts Віллінк, Козловський, Савченко
    /// and Бобрович in that order, and the index has to return Бобрович first
    /// because it sorts by sort name, not by when the row was written.
    /// </summary>
    [Fact]
    public async Task Authors_are_ordered_by_surname_under_Ukrainian_collation()
    {
        var index = await IndexAsync();

        Assert.Equal(
            SeededCatalogue.AuthorSlugsInOrder,
            index.Items.Select(author => author.Slug));
    }

    [Fact]
    public async Task The_index_lists_everyone_credited_on_a_published_book()
    {
        var index = await IndexAsync();

        Assert.Equal(SeededCatalogue.AuthorSlugsInOrder.Length, index.Items.Count);
    }

    /// <summary>
    /// SC-029 and FR-035: whatever their role. A compiler is credited on a book
    /// and so belongs in the index, and the index has to say that is what they
    /// are — leaving it unlabelled would state something untrue about who wrote
    /// the book.
    /// </summary>
    [Fact]
    public async Task A_compiler_appears_in_the_index_with_their_role()
    {
        var index = await IndexAsync();

        var compiler = index.Items.Single(author =>
            author.Slug == SeededCatalogue.CompilerSlug);

        Assert.Contains("compiler", compiler.Roles);
        Assert.Equal(SeededCatalogue.CompilerName, compiler.Name);
    }

    [Fact]
    public async Task Every_entry_carries_a_sort_name_and_at_least_one_role()
    {
        var index = await IndexAsync();

        Assert.All(index.Items, author =>
        {
            Assert.False(string.IsNullOrWhiteSpace(author.SortName));
            Assert.NotEmpty(author.Roles);
            Assert.True(author.BookCount > 0);
        });
    }

    private async Task<AuthorIndexResponse> IndexAsync() =>
        await _client.GetFromJsonAsync<AuthorIndexResponse>("/api/authors")
        ?? throw new InvalidOperationException("The index returned no body.");
}
