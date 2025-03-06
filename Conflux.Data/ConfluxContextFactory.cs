using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

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

        IConfiguration config = new ConfigurationBuilder()
                                .SetBasePath(basePath)
                                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                                .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
                                .Build();

        string? connectionString = config.GetConnectionString("Database");

        DbContextOptionsBuilder<ConfluxContext> optionsBuilder = new();
        optionsBuilder.UseNpgsql(connectionString);

        return new(optionsBuilder.Options);
    }
}
