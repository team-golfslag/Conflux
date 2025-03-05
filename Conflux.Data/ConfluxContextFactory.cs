using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Conflux.Data;

/// <summary>
/// Represents a factory for creating instances of <see cref="ConfluxContext"/>.
/// </summary>
public class ConfluxContextFactory : IDesignTimeDbContextFactory<ConfluxContext>
{
    /// <summary>
    /// Creates a new instance of <see cref="ConfluxContext"/>.
    /// </summary>
    /// <param name="args">The arguments passed to the factory.</param>
    /// <returns>A new instance of <see cref="ConfluxContext"/>.</returns>
    public ConfluxContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<ConfluxContext> optionsBuilder = new();
        
        optionsBuilder.UseNpgsql("Host=localhost:5432;Database=conflux-dev;Username=conflux-dev;Password=conflux-dev"); 
        
        optionsBuilder.UseNpgsql();

        return new(optionsBuilder.Options);
    }
}
