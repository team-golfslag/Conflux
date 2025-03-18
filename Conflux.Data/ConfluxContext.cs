using Conflux.Domain;
using Conflux.RepositoryConnections.NWOpen;
using Microsoft.EntityFrameworkCore;

namespace Conflux.Data;

/// <summary>
/// The database context for Conflux.
/// </summary>
/// <param name="options">The database context options.</param>
public class ConfluxContext(DbContextOptions<ConfluxContext> options) : DbContext(options)
{
    public DbSet<Person> People { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<Party> Parties { get; set; }

    /// <summary>
    /// Configures the relationships between the entities.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Project>().HasMany(p => p.People).WithMany();

        modelBuilder.Entity<Project>().HasMany(p => p.Products).WithMany();

        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Seeds the database with initial data.
    /// </summary>
    public async Task SeedDataAsync()
    {
        TemporaryProjectRetriever projectRetriever = TemporaryProjectRetriever.GetInstance();
        SeedData seedData = projectRetriever.MapProjectsAsync().Result;

        await People.AddRangeAsync(seedData.People);
        await Products.AddRangeAsync(seedData.Products);
        await Parties.AddRangeAsync(seedData.Parties);
        await Projects.AddRangeAsync(seedData.Projects);

        await SaveChangesAsync();
    }
}
