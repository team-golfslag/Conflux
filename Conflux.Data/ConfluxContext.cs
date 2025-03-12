using Conflux.Domain;
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
        Party party = new()
        {
            Name = "Partij",
        };
        Project project = new()
        {
            Title = "Projekt",
        };
        Person person = new()
        {
            Name = "Persoon",
            Age = 5,
        };
        Product product = new()
        {
            Title = "Produkt",
            Url = "https://conflux.com",
        };

        project.People.Add(person);
        project.Products.Add(product);
        project.Parties.Add(party);

        await Projects.AddAsync(project);
        await SaveChangesAsync();
    }
}
