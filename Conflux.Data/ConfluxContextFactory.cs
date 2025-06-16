// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Pgvector.EntityFrameworkCore;

namespace Conflux.Data;

/// <summary>
/// Represents a factory for creating instances of <see cref="ConfluxContext" />.
/// </summary>
public class ConfluxContextFactory : IDesignTimeDbContextFactory<ConfluxContext>
{
    /// <summary>
    /// Creates a new instance of <see cref="ConfluxContext" />.
    /// </summary>
    /// <param name="args">The arguments passed to the factory.</param>
    /// <returns>A new instance of <see cref="ConfluxContext" />.</returns>
    public ConfluxContext CreateDbContext(string[] args)
    {
        string environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        string basePath = Path.Combine(Directory.GetCurrentDirectory(), "../Conflux.API");
        
        // Recursively find solution root if the base path does not exist (for testing)
        if (!Directory.Exists(basePath))
        {
            // Try from the solution root
            var solutionRoot = Directory.GetCurrentDirectory();
            while (solutionRoot != null && !File.Exists(Path.Combine(solutionRoot, "Conflux.sln")))
            {
                solutionRoot = Directory.GetParent(solutionRoot)?.FullName;
            }
            
            if (solutionRoot != null)
            {
                basePath = Path.Combine(solutionRoot, "Conflux.API");
            }
        }

        IConfiguration config = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", false, true)
            .AddJsonFile($"appsettings.{environmentName}.json", true, true)
            .Build();

        string? connectionString = config.GetConnectionString("Database");
        if (string.IsNullOrEmpty(connectionString))
            // If the connection string is not found in the configuration, use environment variables
            connectionString = ConnectionStringHelper.GetConnectionStringFromEnvironment();

        DbContextOptionsBuilder<ConfluxContext> optionsBuilder = new();
        optionsBuilder.UseNpgsql(connectionString, o => o.UseVector());

        return new(optionsBuilder.Options);
    }
}
