using Microsoft.EntityFrameworkCore;
using VoxLib.Dal.Persistence;

namespace VoxLib.Api.Tests.Infrastructure;

/// <summary>
/// The application over a catalogue with nothing in it. The seeder runs at
/// startup and only when the catalogue is empty, so emptying it afterwards is
/// how this state is reached without giving the application a test-only switch.
/// </summary>
public sealed class EmptyCatalogueApiFixture : ApiFixture
{
    protected override async Task PrepareAsync(VoxLibDbContext database)
    {
        // Chapters and the author join rows go with the books, by cascade.
        await database.Books.ExecuteDeleteAsync();
        await database.Authors.ExecuteDeleteAsync();
    }
}
