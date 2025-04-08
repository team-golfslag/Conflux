// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Data;

public class ConnectionStringHelper
{
    /// <summary>
    /// Gets the connection string from environment variables.
    /// </summary>
    /// <returns>The connection string.</returns>
    public static string GetConnectionStringFromEnvironment()
    {
        string? host = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
        string? port = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
        string? database = Environment.GetEnvironmentVariable("DB_NAME");
        string? user = Environment.GetEnvironmentVariable("DB_USER");
        string? password = Environment.GetEnvironmentVariable("DB_PASSWORD");

        return $"Host={host}:{port};Database={database};Username={user};Password={password}";
    }
}
