using System.Net.Http.Json;
using VoxLib.Api.Tests.Infrastructure;

namespace VoxLib.Api.Tests.Catalogue;

/// <summary>
/// SC-028: the catalogue the API serves is the books the project actually holds,
/// and none of the twenty-seven placeholders FR-064 removed.
/// </summary>
[Collection(CatalogueCollection.Name)]
public class CatalogueSeedTests(CatalogueApiFixture fixture)
{
    private readonly HttpClient _client = fixture.CreateClient();

    /// <summary>
    /// Matched in both directions rather than counted. A count alone passes
    /// against the wrong four, which is exactly the failure this exists to
    /// catch: the placeholder catalogue had twenty-seven books and any four of
    /// them would satisfy a length check.
    /// </summary>
    [Fact]
    public async Task The_catalogue_is_exactly_the_books_the_project_holds()
    {
        var titles = (await ListAsync()).Items.Select(book => book.Title).ToArray();

        Assert.Equal(SeededCatalogue.Titles.Order(), titles.Order());
    }

    [Fact]
    public async Task No_placeholder_book_survives()
    {
        var slugs = (await ListAsync()).Items.Select(book => book.Slug).ToArray();

        Assert.DoesNotContain("kobzar-selected-poems", slugs);
        Assert.DoesNotContain("apostol-cherni", slugs);
        Assert.DoesNotContain("lisova-pisnia", slugs);
    }

    /// <summary>
    /// FR-065: the provisional single chapter carries the book's whole running
    /// time, so the figure a reader sees is right from the start rather than
    /// zero until the audio arrives.
    /// </summary>
    [Fact]
    public async Task Every_book_reports_a_running_time_above_zero()
    {
        var page = await ListAsync();

        Assert.All(page.Items, book => Assert.True(
            book.TotalRunningTimeSeconds > 0,
            $"'{book.Slug}' reports no running time."));
    }

    [Fact]
    public async Task The_whole_catalogue_fits_on_one_page()
    {
        var page = await ListAsync();

        Assert.Equal(SeededCatalogue.PublishedCount, page.TotalCount);
        Assert.Equal(SeededCatalogue.PageCount, page.PageCount);
    }

    [Fact]
    public async Task Books_are_ordered_by_title_under_Ukrainian_collation()
    {
        var slugs = (await ListAsync()).Items.Select(book => book.Slug);

        Assert.Equal(SeededCatalogue.SlugsInOrder, slugs);
    }

    private async Task<PagedBooksResponse> ListAsync() =>
        await _client.GetFromJsonAsync<PagedBooksResponse>("/api/books")
        ?? throw new InvalidOperationException("The catalogue returned no body.");
}
