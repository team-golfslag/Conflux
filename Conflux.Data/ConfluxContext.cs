// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain;
using Conflux.Domain.Session;
using Microsoft.EntityFrameworkCore;

namespace Conflux.Data;

/// <summary>
/// The database context for Conflux.
/// </summary>
public class ConfluxContext : DbContext, IConfluxContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfluxContext" /> class.
    /// </summary>
    /// <param name="options">The options for the context.</param>
    public ConfluxContext(DbContextOptions<ConfluxContext> options) : base(options)
    {
    }
    
    public DbSet<Person> People { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<Contributor> Contributors { get; set; }
    public DbSet<ContributorRole> ContributorRoles { get; set; }
    public DbSet<ContributorPosition> ContributorPositions { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<ProjectDescription> ProjectDescriptions { get; set; }
    public DbSet<ProductCategory> ProductCategories { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<ProjectTitle> ProjectTitles { get; set; }
    public DbSet<Organisation> Organisations { get; set; }
    public DbSet<OrganisationRole> OrganisationRoles { get; set; }

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
        modelBuilder.Entity<Project>()
            .HasMany(p => p.Titles)
            .WithMany();
        modelBuilder.Entity<Project>()
            .HasMany(p => p.Descriptions)
            .WithMany();
        modelBuilder.Entity<User>()
            .HasMany(p => p.Roles)
            .WithMany();

        base.OnModelCreating(modelBuilder);
    }

    public bool ShouldSeed() => Users.Find(UserSession.DevelopmentUserId) != null;
}
