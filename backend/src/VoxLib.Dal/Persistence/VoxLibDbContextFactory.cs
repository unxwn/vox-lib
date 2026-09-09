using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace VoxLib.Dal.Persistence;

/// <summary>
/// Lets `dotnet ef` build the model with VoxLib.Dal as its own startup project.
/// Without this the tooling would need the design package in VoxLib.Api, which
/// would put a design-time dependency in the deployed application for no reason.
/// Generating a migration does not connect to the database.
/// </summary>
public sealed class VoxLibDbContextFactory : IDesignTimeDbContextFactory<VoxLibDbContext>
{
    public VoxLibDbContext CreateDbContext(string[] args) =>
        new(
            new DbContextOptionsBuilder<VoxLibDbContext>()
                .UseNpgsql(DatabaseConnection.Resolve())
                .Options);
}
