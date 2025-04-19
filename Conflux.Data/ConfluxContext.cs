// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

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
    public DbSet<User> Users { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<Party> Parties { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<SRAMGroupIdConnection> SRAMGroupIdConnections { get; set; }

    /// <summary>
    /// Configures the relationships between the entities.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Project>()
            .HasMany(p => p.Users)
            .WithMany();
        modelBuilder.Entity<Project>()
            .HasMany(p => p.Products)
            .WithMany();
        modelBuilder.Entity<User>()
            .HasMany(p => p.Roles)
            .WithMany();

        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Seeds the database with initial data.
    /// </summary>
    public async Task SeedDataAsync()
    {
        TemporaryProjectRetriever projectRetriever = TemporaryProjectRetriever.GetInstance();
        SeedData seedData = projectRetriever.MapProjectsAsync().Result;

        await Users.AddRangeAsync(seedData.Users);
        await Products.AddRangeAsync(seedData.Products);
        await Parties.AddRangeAsync(seedData.Parties);
        await Projects.AddRangeAsync(seedData.Projects);

        await SaveChangesAsync();
    }
}
