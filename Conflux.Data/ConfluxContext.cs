using Conflux.Domain;
using Microsoft.EntityFrameworkCore;

namespace Conflux.Data;

public class ConfluxContext(DbContextOptions<ConfluxContext> options) : DbContext(options)
{
    public DbSet<Person> People { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<Party> Parties { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Project>()
                    .HasMany(p => p.People)
                    .WithMany();

        modelBuilder.Entity<Project>()
                    .HasMany(p => p.Products)
                    .WithMany();

        base.OnModelCreating(modelBuilder);
    }
}
