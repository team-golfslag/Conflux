// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain;
using Conflux.Domain.Session;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

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

    public DbSet<ProjectDescription> ProjectDescriptions { get; set; }

    public DbSet<ProjectOrganisation> ProjectOrganisations { get; set; }
    public DbSet<RAiDInfo> RAiDInfos { get; set; }

    public DbSet<Person> People { get; set; }

    public DbSet<User> Users { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<Contributor> Contributors { get; set; }
    public DbSet<ContributorRole> ContributorRoles { get; set; }
    public DbSet<ContributorPosition> ContributorPositions { get; set; }
    public DbSet<Product> Products { get; set; }
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
            .HasMany(p => p.Products)
            .WithOne(p => p.Project)
            .HasForeignKey(p => p.ProjectId);
        modelBuilder.Entity<Project>()
            .HasMany(p => p.Titles)
            .WithOne(t => t.Project)
            .HasForeignKey(t => t.ProjectId);
        modelBuilder.Entity<Project>()
            .HasMany(p => p.Descriptions)
            .WithOne(d => d.Project)
            .HasForeignKey(d => d.ProjectId);
        modelBuilder.Entity<Project>()
            .HasOne(p => p.RAiDInfo)
            .WithOne(i => i.Project)
            .HasForeignKey<RAiDInfo>(i => i.ProjectId);
        modelBuilder.Entity<Project>()
            .HasMany(p => p.Contributors)
            .WithOne(c => c.Project)
            .HasForeignKey(c => c.ProjectId);
        modelBuilder.Entity<Project>()
            .HasMany(p => p.Organisations)
            .WithOne(o => o.Project)
            .HasForeignKey(o => o.ProjectId);
        modelBuilder.Entity<Project>() //TODO: make this better
            .HasMany(p => p.Users)
            .WithMany();

        modelBuilder.Entity<Person>()
            .HasMany(p => p.Contributors)
            .WithOne(c => c.Person)
            .HasForeignKey(c => c.PersonId);
        modelBuilder.Entity<Contributor>()
            .HasMany(c => c.Positions)
            .WithOne(p => p.Contributor)
            .HasForeignKey(p => new
            {
                p.PersonId,
                p.ProjectId,
            });
        modelBuilder.Entity<Contributor>()
            .HasMany(c => c.Roles)
            .WithOne(r => r.Contributor)
            .HasForeignKey(r => new
            {
                r.PersonId,
                r.ProjectId,
            });
        modelBuilder.Entity<ProjectOrganisation>()
            .HasMany(o => o.Roles)
            .WithOne(r => r.Organisation)
            .HasForeignKey(r => new
            {
                r.ProjectId,
                r.OrganisationId,
            });
        modelBuilder.Entity<Organisation>()
            .HasMany(o => o.Projects)
            .WithOne(p => p.Organisation)
            .HasForeignKey(p => p.OrganisationId);
        modelBuilder.Entity<User>()
            .HasMany(p => p.Roles)
            .WithMany();

        // Configuration for Product.Categories
        modelBuilder.Entity<Product>(entity =>
        {
            // For PostgreSQL, Npgsql can map List<ProductCategoryType> to an integer[] column.
            // However, providing a ValueComparer is still needed for correct change tracking.
            entity.Property(p => p.Categories)
                .Metadata.SetValueComparer(new ValueComparer<List<ProductCategoryType>>(
                    (c1, c2) => c1 == null && c2 == null || c1 != null && c2 != null && c1.SequenceEqual(c2),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()));
        });

        base.OnModelCreating(modelBuilder);
    }

    public bool ShouldSeed() => Users.Find(UserSession.DevelopmentUserId) != null;
}
