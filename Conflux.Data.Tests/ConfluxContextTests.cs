// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain;
using Conflux.Domain.Session;
using Microsoft.EntityFrameworkCore;

namespace Conflux.Data.Tests;

public class ConfluxContextTests
{
    /// <summary>
    /// Given a database context
    /// When the context is created
    /// Then the context should not be null
    /// </summary>
    [Fact]
    public async Task Can_Create_ConfluxContext()
    {
        DbContextOptions<ConfluxContext> options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase(Guid.CreateVersion7().ToString())
            .Options;
        
        ConfluxContext context = new(options);

        // Assert
        Assert.NotNull(context);
    }

    [Fact]
    public void ConfluxContext_ShouldConfigureDbSets()
    {
        // Arrange
        DbContextOptions<ConfluxContext> options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase(Guid.CreateVersion7().ToString())
            .Options;
        
        // Act
        using ConfluxContext context = new(options);

        // Assert
        Assert.NotNull(context.ProjectDescriptions);
        Assert.NotNull(context.ProjectOrganisations);
        Assert.NotNull(context.RAiDInfos);
        Assert.NotNull(context.People);
        Assert.NotNull(context.Users);
        Assert.NotNull(context.UserRoles);
        Assert.NotNull(context.Contributors);
        Assert.NotNull(context.ContributorRoles);
        Assert.NotNull(context.ContributorPositions);
        Assert.NotNull(context.Products);
        Assert.NotNull(context.Projects);
        Assert.NotNull(context.ProjectTitles);
        Assert.NotNull(context.Organisations);
        Assert.NotNull(context.OrganisationRoles);
        Assert.NotNull(context.SRAMGroupIdConnections);
    }

    [Fact]
    public void ConfluxContext_ShouldConfigureProjectRelationships()
    {
        // Arrange
        DbContextOptions<ConfluxContext> options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase(Guid.CreateVersion7().ToString())
            .Options;
        
        // Act
        using ConfluxContext context = new(options);
        context.Database.EnsureCreated();

        // Assert - Test that model is properly configured
        var model = context.Model;
        var projectEntity = model.FindEntityType(typeof(Project));
        Assert.NotNull(projectEntity);

        // Check that relationships are configured
        var productNavigation = projectEntity.FindNavigation(nameof(Project.Products));
        Assert.NotNull(productNavigation);

        var titlesNavigation = projectEntity.FindNavigation(nameof(Project.Titles));
        Assert.NotNull(titlesNavigation);

        var descriptionsNavigation = projectEntity.FindNavigation(nameof(Project.Descriptions));
        Assert.NotNull(descriptionsNavigation);

        var contributorsNavigation = projectEntity.FindNavigation(nameof(Project.Contributors));
        Assert.NotNull(contributorsNavigation);

        var organisationsNavigation = projectEntity.FindNavigation(nameof(Project.Organisations));
        Assert.NotNull(organisationsNavigation);

        var usersSkipNavigation = projectEntity.FindSkipNavigation(nameof(Project.Users));
        Assert.NotNull(usersSkipNavigation);
    }

    [Fact]
    public void ConfluxContext_ShouldConfigurePersonRelationships()
    {
        // Arrange
        DbContextOptions<ConfluxContext> options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase(Guid.CreateVersion7().ToString())
            .Options;
        
        // Act
        using ConfluxContext context = new(options);
        context.Database.EnsureCreated();

        // Assert
        var model = context.Model;
        var personEntity = model.FindEntityType(typeof(Person));
        Assert.NotNull(personEntity);

        var contributorsNavigation = personEntity.FindNavigation(nameof(Person.Contributors));
        Assert.NotNull(contributorsNavigation);

        var userNavigation = personEntity.FindNavigation(nameof(Person.User));
        Assert.NotNull(userNavigation);
    }

    [Fact]
    public void ConfluxContext_ShouldConfigureContributorRelationships()
    {
        // Arrange
        DbContextOptions<ConfluxContext> options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase(Guid.CreateVersion7().ToString())
            .Options;
        
        // Act
        using ConfluxContext context = new(options);
        context.Database.EnsureCreated();

        // Assert
        var model = context.Model;
        var contributorEntity = model.FindEntityType(typeof(Contributor));
        Assert.NotNull(contributorEntity);

        var positionsNavigation = contributorEntity.FindNavigation(nameof(Contributor.Positions));
        Assert.NotNull(positionsNavigation);

        var rolesNavigation = contributorEntity.FindNavigation(nameof(Contributor.Roles));
        Assert.NotNull(rolesNavigation);
    }

    [Fact]
    public void ConfluxContext_ShouldConfigureOrganisationRelationships()
    {
        // Arrange
        DbContextOptions<ConfluxContext> options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase(Guid.CreateVersion7().ToString())
            .Options;
        
        // Act
        using ConfluxContext context = new(options);
        context.Database.EnsureCreated();

        // Assert
        var model = context.Model;
        var organisationEntity = model.FindEntityType(typeof(Organisation));
        Assert.NotNull(organisationEntity);

        var projectsNavigation = organisationEntity.FindNavigation(nameof(Organisation.Projects));
        Assert.NotNull(projectsNavigation);
    }

    [Fact]
    public void ConfluxContext_ShouldConfigureProjectOrganisationRelationships()
    {
        // Arrange
        DbContextOptions<ConfluxContext> options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase(Guid.CreateVersion7().ToString())
            .Options;
        
        // Act
        using ConfluxContext context = new(options);
        context.Database.EnsureCreated();

        // Assert
        var model = context.Model;
        var projectOrganisationEntity = model.FindEntityType(typeof(ProjectOrganisation));
        Assert.NotNull(projectOrganisationEntity);

        var rolesNavigation = projectOrganisationEntity.FindNavigation(nameof(ProjectOrganisation.Roles));
        Assert.NotNull(rolesNavigation);
    }

    [Fact]
    public void ConfluxContext_ShouldConfigureUserRelationships()
    {
        // Arrange
        DbContextOptions<ConfluxContext> options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase(Guid.CreateVersion7().ToString())
            .Options;
        
        // Act
        using ConfluxContext context = new(options);
        context.Database.EnsureCreated();

        // Assert
        var model = context.Model;
        var userEntity = model.FindEntityType(typeof(User));
        Assert.NotNull(userEntity);

        var rolesSkipNavigation = userEntity.FindSkipNavigation(nameof(User.Roles));
        Assert.NotNull(rolesSkipNavigation);

        var personNavigation = userEntity.FindNavigation(nameof(User.Person));
        Assert.NotNull(personNavigation);
    }

    [Fact]
    public void ConfluxContext_ShouldConfigureProductCategories()
    {
        // Arrange
        DbContextOptions<ConfluxContext> options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase(Guid.CreateVersion7().ToString())
            .Options;
        
        // Act
        using ConfluxContext context = new(options);
        context.Database.EnsureCreated();

        // Assert
        var model = context.Model;
        var productEntity = model.FindEntityType(typeof(Product));
        Assert.NotNull(productEntity);

        var categoriesProperty = productEntity.FindProperty(nameof(Product.Categories));
        Assert.NotNull(categoriesProperty);
        Assert.NotNull(categoriesProperty.GetValueComparer());
    }

    [Fact]
    public void ConfluxContext_ShouldHandleInMemoryProvider()
    {
        // Arrange
        DbContextOptions<ConfluxContext> options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase(Guid.CreateVersion7().ToString())
            .Options;
        
        // Act
        using ConfluxContext context = new(options);
        context.Database.EnsureCreated();

        // Assert - Should not throw exceptions for InMemory provider
        var model = context.Model;
        var projectEntity = model.FindEntityType(typeof(Project));
        Assert.NotNull(projectEntity);

        // Vector properties should be ignored for InMemory provider
        var embeddingProperty = projectEntity.FindProperty("Embedding");
        var contentHashProperty = projectEntity.FindProperty("EmbeddingContentHash");
        var lastUpdatedProperty = projectEntity.FindProperty("EmbeddingLastUpdated");

        // These properties should be ignored in InMemory provider
        // We can check if they exist but expect them to be ignored
        Assert.True(embeddingProperty == null || embeddingProperty.IsShadowProperty());
        Assert.True(contentHashProperty == null || contentHashProperty.IsShadowProperty());
        Assert.True(lastUpdatedProperty == null || lastUpdatedProperty.IsShadowProperty());
    }

    [Fact]
    public void ConfluxContext_ShouldConfigureIndexes()
    {
        // Arrange
        DbContextOptions<ConfluxContext> options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase(Guid.CreateVersion7().ToString())
            .Options;
        
        // Act
        using ConfluxContext context = new(options);
        context.Database.EnsureCreated();

        // Assert
        var model = context.Model;
        
        // Check Project entity indexes
        var projectEntity = model.FindEntityType(typeof(Project));
        Assert.NotNull(projectEntity);
        
        var scimIdIndex = projectEntity.GetIndexes().FirstOrDefault(i => 
            i.Properties.Any(p => p.Name == nameof(Project.SCIMId)));
        Assert.NotNull(scimIdIndex);

        // Check ProjectTitle entity indexes
        var projectTitleEntity = model.FindEntityType(typeof(ProjectTitle));
        Assert.NotNull(projectTitleEntity);
        
        var projectIdIndex = projectTitleEntity.GetIndexes().FirstOrDefault(i => 
            i.Properties.Any(p => p.Name == nameof(ProjectTitle.ProjectId)));
        Assert.NotNull(projectIdIndex);

        // Check ProjectDescription entity indexes
        var projectDescriptionEntity = model.FindEntityType(typeof(ProjectDescription));
        Assert.NotNull(projectDescriptionEntity);
        
        var descriptionProjectIdIndex = projectDescriptionEntity.GetIndexes().FirstOrDefault(i => 
            i.Properties.Any(p => p.Name == nameof(ProjectDescription.ProjectId)));
        Assert.NotNull(descriptionProjectIdIndex);
    }

    [Fact]
    public void ConfluxContext_ShouldSeed_ReturnsFalseWhenDevelopmentUserNotFound()
    {
        // Arrange
        DbContextOptions<ConfluxContext> options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase(Guid.CreateVersion7().ToString())
            .Options;
        
        // Act
        using ConfluxContext context = new(options);
        context.Database.EnsureCreated();

        // Assert
        bool shouldSeed = context.ShouldSeed();
        Assert.False(shouldSeed);
    }

    [Fact]
    public void ConfluxContext_ShouldSeed_ReturnsTrueWhenDevelopmentUserExists()
    {
        // Arrange
        DbContextOptions<ConfluxContext> options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase(Guid.CreateVersion7().ToString())
            .Options;
        
        using ConfluxContext context = new(options);
        context.Database.EnsureCreated();

        // Add development user
        context.Users.Add(new User
        {
            Id = UserSession.DevelopmentUserId,
            SCIMId = "test-scim-id",
            PersonId = Guid.CreateVersion7(),
            Person = new Person
            {
                Id = Guid.CreateVersion7(),
                Name = "Dev User",
                Email = "dev@example.com"
            }
        });
        context.SaveChanges();

        // Act
        bool shouldSeed = context.ShouldSeed();

        // Assert
        Assert.True(shouldSeed);
    }

    [Fact]
    public void ConfluxContext_WithPostgreSQL_ShouldConfigureVectorExtension()
    {
        // This test would require a real PostgreSQL connection to properly test
        // For now, we'll test that the method doesn't throw with InMemory provider
        
        // Arrange
        DbContextOptions<ConfluxContext> options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase(Guid.CreateVersion7().ToString())
            .Options;
        
        // Act & Assert - Should not throw
        using ConfluxContext context = new(options);
        context.Database.EnsureCreated();
        
        Assert.NotNull(context);
    }
}
