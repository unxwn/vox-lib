namespace VoxLib.Dal.Persistence;

/// <summary>
/// Where the connection string comes from. One place, because the API, the
/// design-time tooling and the test harness all have to agree on it.
/// </summary>
public static class DatabaseConnection
{
    public const string EnvironmentVariable = "VOXLIB_DB_CONNECTION";

    /// <summary>
    /// Matches the db service in .devcontainer/docker-compose.yml, so a checkout
    /// with the container running needs no further configuration. It is a local
    /// development credential and is not used anywhere a real one would be.
    /// </summary>
    public const string LocalDefault =
        "Host=localhost;Port=5432;Database=voxlib;Username=voxlib;Password=voxlib";

    public static string Resolve() =>
        Environment.GetEnvironmentVariable(EnvironmentVariable) is { Length: > 0 } fromEnvironment
            ? fromEnvironment
            : LocalDefault;
}
