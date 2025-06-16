// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain;
using Conflux.Domain.Session;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Pgvector.EntityFrameworkCore;

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
        // Check if we're using PostgreSQL or InMemory provider
        bool isPostgreSQL = Database.IsNpgsql();
        bool isInMemory = Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";

        // Only configure PostgreSQL-specific features when using PostgreSQL
        if (isPostgreSQL)
        {
            // Register the Pgvector extension for vector support
            modelBuilder.HasPostgresExtension("vector");
            // Enable trigram extension for text search
            modelBuilder.HasPostgresExtension("pg_trgm");
        }

        // Configure basic entity relationships (works for all providers)
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
        modelBuilder.Entity<User>()
            .HasOne(u => u.Person)
            .WithOne(p => p.User)
            .HasForeignKey<User>(u => u.PersonId);
        
        
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

        modelBuilder.Entity<Project>(entity =>
        {
            if (isPostgreSQL)
            {
                // Create HNSW index on embedding column for fast vector similarity search
                entity.HasIndex(p => p.Embedding)
                    .HasMethod("hnsw")
                    .HasOperators("vector_cosine_ops")
                    .HasStorageParameter("m", 16)
                    .HasStorageParameter("ef_construction", 64);
            }
            else if (isInMemory)
            {
                // Ignore vector properties for InMemory provider since it doesn't support them
                entity.Ignore(p => p.Embedding);
                entity.Ignore(p => p.EmbeddingContentHash);
                entity.Ignore(p => p.EmbeddingLastUpdated);
            }
                
            // Index on SCIMId for faster access control queries (works for all providers)
            entity.HasIndex(p => p.SCIMId);
        });

        if (isPostgreSQL)
        {
            // Add indexes for text search performance (PostgreSQL specific)
            modelBuilder.Entity<ProjectTitle>(entity =>
            {
                // GIN index on Text column for fast text search using LIKE operations
                entity.HasIndex(pt => pt.Text)
                    .HasMethod("gin")
                    .HasOperators("gin_trgm_ops");
                    
                // Regular index for foreign key lookups
                entity.HasIndex(pt => pt.ProjectId);
            });

            modelBuilder.Entity<ProjectDescription>(entity =>
            {
                // GIN index on Text column for fast text search using LIKE operations
                entity.HasIndex(pd => pd.Text)
                    .HasMethod("gin")
                    .HasOperators("gin_trgm_ops");
                    
                // Regular index for foreign key lookups
                entity.HasIndex(pd => pd.ProjectId);
            });
        }
        else
        {
            // Add basic indexes for other providers
            modelBuilder.Entity<ProjectTitle>(entity =>
            {
                entity.HasIndex(pt => pt.ProjectId);
            });

            modelBuilder.Entity<ProjectDescription>(entity =>
            {
                entity.HasIndex(pd => pd.ProjectId);
            });
        }

        base.OnModelCreating(modelBuilder);
    }

    public bool ShouldSeed() => Users.Find(UserSession.DevelopmentUserId) != null;
}
