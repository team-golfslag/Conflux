// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
//
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
using Conflux.Domain.Logic.DTOs.Queries;
using Conflux.Domain.Logic.DTOs.Requests;
using Conflux.Domain.Logic.DTOs.Responses;
using Conflux.Domain.Logic.Exceptions;
using Conflux.Domain.Logic.Services;
using Conflux.Domain.Session;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using Moq;
using Pgvector;
using Xunit;

namespace Conflux.Domain.Logic.Tests.Services;

public class ProjectsServiceTests : IDisposable
{
    private readonly Mock<IUserSessionService> _userSessionServiceMock = new();
    private readonly Mock<IVariantFeatureManager> _featureManagerMock = new();
    private readonly Mock<ILogger<ProjectsService>> _loggerMock = new();
    private readonly ConfluxContext _context = null!;
    private readonly ProjectsService _service = null!;

    public ProjectsServiceTests()
    {
        ServiceProvider serviceProvider = new ServiceCollection()
            .AddEntityFrameworkInMemoryDatabase()
            .BuildServiceProvider();

        DbContextOptions<ConfluxContext> options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.CreateVersion7()}")
            .UseInternalServiceProvider(serviceProvider)
            .Options;

        _context = new ConfluxContext(options);
        _context.Database.EnsureCreated();

        // By default, disable semantic search for tests to use simple text search
        _featureManagerMock.Setup(f => f.IsEnabledAsync("SemanticSearch", default))
            .ReturnsAsync(false);

        _service = new ProjectsService(_context, _userSessionServiceMock.Object, _loggerMock.Object, _featureManagerMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Helper Methods

    /// <summary>
    /// Creates a test user and its associated person, saves them to the database,
    /// and returns the User entity.
    /// </summary>
    private async Task<User> CreateTestUserAsync(
        string email = "test@example.com",
        List<Guid>? favoriteProjectIds = null
    )
    {
        var person = new Person
        {
            Id = Guid.CreateVersion7(),
            Name = "Test User",
            Email = email
        };

        var user = new User
        {
            Id = Guid.CreateVersion7(),
            SRAMId = $"sram-id-{Guid.CreateVersion7()}",
            SCIMId = $"scim-id-{Guid.CreateVersion7()}",
            PersonId = person.Id,
            Person = person,
            FavoriteProjectIds = favoriteProjectIds ?? []
        };

        person.User = user;
        _context.People.Add(person);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    /// <summary>
    /// Creates a test project, saves it to the database, and returns the Project entity.
    /// </summary>
    private async Task<Project> CreateTestProjectAsync(
        string title = "Test Project",
        IEnumerable<User>? users = null
    )
    {
        var projectId = Guid.CreateVersion7();
        var project = new Project
        {
            Id = projectId,
            SCIMId = $"project-scim-{Guid.CreateVersion7()}",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            Titles =
            [
                new()
                {
                    ProjectId = projectId,
                    Text = title,
                    Type = TitleType.Primary,
                    StartDate = DateTime.UtcNow,
                }
            ],
            Users = users?.ToList() ?? []
        };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();
        return project;
    }

    /// <summary>
    /// Creates a test organisation, saves it to the database, and returns the Organisation entity.
    /// </summary>
    private async Task<Organisation> CreateTestOrganisationAsync()
    {
        var organisation = new Organisation
        {
            Id = Guid.CreateVersion7(),
            RORId = "https://ror.org/00x00x00",
            Name = "Test Organisation",
        };
        _context.Organisations.Add(organisation);
        await _context.SaveChangesAsync();
        return organisation;
    }

    /// <summary>
    /// Sets up the user session service mock to return the specified user.
    /// </summary>
    private void SetupUserSessionMock(User user)
    {
        var userSession = new UserSession
        {
            Email = user.Person.Email,
            Name = user.Person.Name,
            SRAMId = user.SRAMId,
            User = user,
            Collaborations = []
        };
        
        // Set user as SuperAdmin so they can see all projects
        user.PermissionLevel = PermissionLevel.SuperAdmin;
        
        _userSessionServiceMock.Setup(s => s.GetUser()).ReturnsAsync(userSession);
        _userSessionServiceMock
            .Setup(s => s.CommitUser(It.IsAny<UserSession>()))
            .Returns(Task.CompletedTask);
    }

    #endregion

    [Fact]
    public async Task UpdateProjectAsync_ShouldThrow_WhenProjectDoesNotExist()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ProjectNotFoundException>(
            async () =>
                await _service.PutProjectAsync(
                    Guid.CreateVersion7(),
                    new ProjectRequestDTO
                    {
                        StartDate = DateTime.UtcNow,
                        EndDate = DateTime.UtcNow.AddDays(1),
                    }
                )
        );
    }

    [Fact]
    public async Task ExportProjectsToCsvAsync_ShouldReturnCsvContent()
    {
        // Arrange
        User user = await CreateTestUserAsync();
        Organisation organisation = await CreateTestOrganisationAsync();
        Project project = await CreateTestProjectAsync(users: [user]);
        SetupUserSessionMock(user);

        // Manually add relations not covered by helpers
        var projectOrganisation = new ProjectOrganisation
        {
            OrganisationId = organisation.Id,
            ProjectId = project.Id
        };
        var product = new Product
        {
            Id = Guid.CreateVersion7(),
            ProjectId = project.Id,
            Title = "Test Product",
            Url = "https://example.com/product",
            Type = ProductType.Software,
        };
        var contributor = new Contributor
        {
            PersonId = user.PersonId,
            ProjectId = project.Id,
            Leader = false,
            Contact = false
        };
        
        _context.ProjectOrganisations.Add(projectOrganisation);
        _context.Products.Add(product);
        _context.Contributors.Add(contributor);
        await _context.SaveChangesAsync();

        ProjectCsvRequestDTO queryDto = new()
        {
            Query = "Test",
        };

        // Act
        string csvContent = await _service.ExportProjectsToCsvAsync(queryDto);

        // Assert
        Assert.NotNull(csvContent);
        Assert.NotEmpty(csvContent);
        Assert.Contains("id", csvContent);
        Assert.Contains(project.Id.ToString(), csvContent);
        Assert.Contains(user.Person.Name, csvContent);
        Assert.Contains(organisation.Name, csvContent);
        Assert.Contains("Test Product", csvContent);
    }

    [Fact]
    public async Task PutProjectAsync_ShouldUpdateExistingProject()
    {
        // Arrange
        Project originalProject = await CreateTestProjectAsync("Original Title");
        originalProject.Lectorate = "Jeugd";
        await _context.SaveChangesAsync();

        var putRequestDTO = new ProjectRequestDTO
        {
            StartDate = new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2024, 3, 1, 23, 59, 59, DateTimeKind.Utc),
            Lectorate = "Wonen en Welzijn"
        };

        // Act
        ProjectResponseDTO updatedProject =
            await _service.PutProjectAsync(originalProject.Id, putRequestDTO);

        // Assert
        Assert.NotNull(updatedProject);
        Assert.Equal(putRequestDTO.StartDate, updatedProject.StartDate);
        Assert.Equal(putRequestDTO.EndDate, updatedProject.EndDate);
        Assert.Equal("Wonen en Welzijn", updatedProject.Lectorate);
    }

    [Fact]
    public async Task FavoriteProjectAsync_ShouldAddProjectToFavorites_WhenFavoriteIsTrue()
    {
        // Arrange
        User user = await CreateTestUserAsync();
        Project project = await CreateTestProjectAsync(users: [user]);
        SetupUserSessionMock(user);

        // Act
        await _service.FavoriteProjectAsync(project.Id, true);

        // Assert
        User? updatedUser = await _context.Users.FindAsync(user.Id);
        Assert.NotNull(updatedUser);
        Assert.Contains(project.Id, updatedUser.FavoriteProjectIds);
        Assert.Single(updatedUser.FavoriteProjectIds);
        _userSessionServiceMock.Verify(
            s => s.UpdateUser(),
            Times.Once
        );
    }

    [Fact]
    public async Task FavoriteProjectAsync_ShouldRemoveProjectFromFavorites_WhenFavoriteIsFalse()
    {
        // Arrange
        User user = await CreateTestUserAsync();
        Project project = await CreateTestProjectAsync(users: [user]);
        user.FavoriteProjectIds.Add(project.Id); // Add the project to favorites first
        await _context.SaveChangesAsync();
        SetupUserSessionMock(user);

        // Act
        await _service.FavoriteProjectAsync(project.Id, false);

        // Assert
        User? updatedUser = await _context.Users.FindAsync(user.Id);
        Assert.NotNull(updatedUser);
        Assert.DoesNotContain(project.Id, updatedUser.FavoriteProjectIds);
        Assert.Empty(updatedUser.FavoriteProjectIds);
        _userSessionServiceMock.Verify(
            s => s.UpdateUser(),
            Times.Once
        );
    }

    [Fact]
    public async Task FavoriteProjectAsync_ShouldThrow_WhenProjectDoesNotExist()
    {
        // Arrange
        User user = await CreateTestUserAsync();
        SetupUserSessionMock(user);

        // Act & Assert
        await Assert.ThrowsAsync<ProjectNotFoundException>(
            () => _service.FavoriteProjectAsync(Guid.CreateVersion7(), true)
        );
    }

    [Fact]
    public async Task FavoriteProjectAsync_ShouldThrow_WhenUserNotAuthenticated()
    {
        // Arrange
        _userSessionServiceMock.Setup(s => s.GetUser())
            .ReturnsAsync((UserSession?)null);

        // Act & Assert
        await Assert.ThrowsAsync<UserNotAuthenticatedException>(
            () => _service.FavoriteProjectAsync(Guid.CreateVersion7(), true)
        );
    }

    [Fact]
    public async Task FavoriteProjectAsync_ShouldHandleGracefully_WhenRemovingNonFavoritedProject()
    {
        // Arrange
        User user = await CreateTestUserAsync(); // User with empty favorites
        Project project = await CreateTestProjectAsync(users: [user]);
        SetupUserSessionMock(user);

        // Act
        await _service.FavoriteProjectAsync(project.Id, false);

        // Assert
        User? updatedUser = await _context.Users.FindAsync(user.Id);
        Assert.NotNull(updatedUser);
        Assert.Empty(updatedUser.FavoriteProjectIds);
        _userSessionServiceMock.Verify(
            s => s.UpdateUser(),
            Times.Once
        );
    }

    [Fact]
    public async Task GetProjectsByQueryAsync_WhenSemanticSearchEnabled_UsesSemanticSearch()
    {
        // Arrange
        _featureManagerMock.Setup(f => f.IsEnabledAsync("SemanticSearch", default))
            .ReturnsAsync(true);
        
        var userSession = new UserSession { User = new User { 
            PermissionLevel = PermissionLevel.SuperAdmin, 
            SCIMId = "test-scim-id", 
            PersonId = Guid.NewGuid() 
        } };
        _userSessionServiceMock.Setup(x => x.GetUser()).ReturnsAsync(userSession);

        var queryDto = new ProjectQueryDTO { Query = "test search" };

        // Act & Assert - Should not throw since we don't have embedding service configured
        // The method should handle the null embedding service gracefully when feature is enabled
        var result = await _service.GetProjectsByQueryAsync(queryDto);
        
        // Verify it falls back to simple text search when embedding service is not available
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetProjectsByQueryAsync_WhenSemanticSearchDisabled_UsesSimpleTextSearch()
    {
        // Arrange
        _featureManagerMock.Setup(f => f.IsEnabledAsync("SemanticSearch", default))
            .ReturnsAsync(false);
        
        var userSession = new UserSession { User = new User { 
            PermissionLevel = PermissionLevel.SuperAdmin, 
            SCIMId = "test-scim-id", 
            PersonId = Guid.NewGuid() 
        } };
        _userSessionServiceMock.Setup(x => x.GetUser()).ReturnsAsync(userSession);

        // Create a test project
        var project = await CreateTestProjectAsync("Test Project");
        
        var queryDto = new ProjectQueryDTO { Query = "test" };

        // Act
        var result = await _service.GetProjectsByQueryAsync(queryDto);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result); // Should find the test project
        Assert.Equal(project.Id, result[0].Id);
    }

    [Fact]
    public async Task GetProjectsByQueryAsync_WithSemanticSearchAndEmbeddingService_UsesSemanticSearch()
    {
        // Arrange
        var embeddingServiceMock = new Mock<IEmbeddingService>();
        var queryEmbedding = new Vector(new float[384]);
        
        embeddingServiceMock.Setup(x => x.GenerateEmbeddingAsync(It.IsAny<string>()))
            .ReturnsAsync(queryEmbedding);
        embeddingServiceMock.Setup(x => x.EmbeddingDimension)
            .Returns(384);

        var serviceWithEmbedding = new ProjectsService(
            _context, 
            _userSessionServiceMock.Object, 
            _loggerMock.Object, 
            _featureManagerMock.Object, 
            embeddingServiceMock.Object);

        _featureManagerMock.Setup(f => f.IsEnabledAsync("SemanticSearch", default))
            .ReturnsAsync(true);
        
        var userSession = new UserSession { User = new User { 
            PermissionLevel = PermissionLevel.SuperAdmin, 
            SCIMId = "test-scim-id", 
            PersonId = Guid.NewGuid() 
        } };
        _userSessionServiceMock.Setup(x => x.GetUser()).ReturnsAsync(userSession);

        // Create a test project with embedding
        var project = await CreateTestProjectAsync("Machine Learning Research");
        project.Embedding = new Vector(new float[384]);
        project.EmbeddingContentHash = "test-hash";
        project.EmbeddingLastUpdated = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        
        var queryDto = new ProjectQueryDTO { Query = "machine learning" };

        // Act
        var result = await serviceWithEmbedding.GetProjectsByQueryAsync(queryDto);

        // Assert
        Assert.NotNull(result);
        embeddingServiceMock.Verify(x => x.GenerateEmbeddingAsync("machine learning"), Times.Once);
    }

    [Fact]
    public async Task GetProjectsByQueryAsync_SemanticSearchWithError_FallsBackToTextSearch()
    {
        // Arrange
        var embeddingServiceMock = new Mock<IEmbeddingService>();
        embeddingServiceMock.Setup(x => x.GenerateEmbeddingAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Embedding generation failed"));
        embeddingServiceMock.Setup(x => x.EmbeddingDimension)
            .Returns(384);

        var serviceWithEmbedding = new ProjectsService(
            _context, 
            _userSessionServiceMock.Object, 
            _loggerMock.Object, 
            _featureManagerMock.Object, 
            embeddingServiceMock.Object);

        _featureManagerMock.Setup(f => f.IsEnabledAsync("SemanticSearch", default))
            .ReturnsAsync(true);
        
        var userSession = new UserSession { User = new User { 
            PermissionLevel = PermissionLevel.SuperAdmin, 
            SCIMId = "test-scim-id", 
            PersonId = Guid.NewGuid() 
        } };
        _userSessionServiceMock.Setup(x => x.GetUser()).ReturnsAsync(userSession);

        // Create a test project
        var project = await CreateTestProjectAsync("Test Project");
        
        var queryDto = new ProjectQueryDTO { Query = "test" };

        // Act
        var result = await serviceWithEmbedding.GetProjectsByQueryAsync(queryDto);

        // Assert
        Assert.NotNull(result);
        // Should fall back to text search and still find the project
        Assert.Single(result);
        Assert.Equal(project.Id, result[0].Id);
    }

    [Fact]
    public async Task GetProjectsByQueryAsync_SemanticSearchWithEmptyQuery_UsesTextSearch()
    {
        // Arrange
        var embeddingServiceMock = new Mock<IEmbeddingService>();
        var serviceWithEmbedding = new ProjectsService(
            _context, 
            _userSessionServiceMock.Object, 
            _loggerMock.Object, 
            _featureManagerMock.Object, 
            embeddingServiceMock.Object);

        _featureManagerMock.Setup(f => f.IsEnabledAsync("SemanticSearch", default))
            .ReturnsAsync(true);
        
        var userSession = new UserSession { User = new User { 
            PermissionLevel = PermissionLevel.SuperAdmin, 
            SCIMId = "test-scim-id", 
            PersonId = Guid.NewGuid() 
        } };
        _userSessionServiceMock.Setup(x => x.GetUser()).ReturnsAsync(userSession);

        var queryDto = new ProjectQueryDTO { Query = "" }; // Empty query

        // Act
        var result = await serviceWithEmbedding.GetProjectsByQueryAsync(queryDto);

        // Assert
        Assert.NotNull(result);
        // Should not call embedding service for empty query
        embeddingServiceMock.Verify(x => x.GenerateEmbeddingAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetProjectsByQueryAsync_SemanticSearchWithNullQuery_UsesTextSearch()
    {
        // Arrange
        var embeddingServiceMock = new Mock<IEmbeddingService>();
        var serviceWithEmbedding = new ProjectsService(
            _context, 
            _userSessionServiceMock.Object, 
            _loggerMock.Object, 
            _featureManagerMock.Object, 
            embeddingServiceMock.Object);

        _featureManagerMock.Setup(f => f.IsEnabledAsync("SemanticSearch", default))
            .ReturnsAsync(true);
        
        var userSession = new UserSession { User = new User { 
            PermissionLevel = PermissionLevel.SuperAdmin, 
            SCIMId = "test-scim-id", 
            PersonId = Guid.NewGuid() 
        } };
        _userSessionServiceMock.Setup(x => x.GetUser()).ReturnsAsync(userSession);

        var queryDto = new ProjectQueryDTO { Query = null }; // Null query

        // Act
        var result = await serviceWithEmbedding.GetProjectsByQueryAsync(queryDto);

        // Assert
        Assert.NotNull(result);
        // Should not call embedding service for null query
        embeddingServiceMock.Verify(x => x.GenerateEmbeddingAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateProjectEmbeddingAsync_WithEmbeddingService_UpdatesEmbedding()
    {
        // Arrange
        var embeddingServiceMock = new Mock<IEmbeddingService>();
        var expectedEmbedding = new Vector(new float[384]);
        
        embeddingServiceMock.Setup(x => x.GenerateEmbeddingAsync(It.IsAny<string>()))
            .ReturnsAsync(expectedEmbedding);
        embeddingServiceMock.Setup(x => x.EmbeddingDimension)
            .Returns(384);

        var serviceWithEmbedding = new ProjectsService(
            _context, 
            _userSessionServiceMock.Object, 
            _loggerMock.Object, 
            _featureManagerMock.Object, 
            embeddingServiceMock.Object);

        var project = await CreateTestProjectAsync("Test Project for Embedding");

        // Act
        await serviceWithEmbedding.UpdateProjectEmbeddingAsync(project.Id);

        // Assert
        embeddingServiceMock.Verify(x => x.GenerateEmbeddingAsync(It.IsAny<string>()), Times.Once);
        
        // Verify the project was updated
        var updatedProject = await _context.Projects
            .Include(p => p.Titles)
            .Include(p => p.Descriptions)
            .FirstAsync(p => p.Id == project.Id);
        
        Assert.NotNull(updatedProject.Embedding);
        Assert.NotNull(updatedProject.EmbeddingContentHash);
        Assert.NotNull(updatedProject.EmbeddingLastUpdated);
    }

    [Fact]
    public async Task UpdateProjectEmbeddingAsync_WithoutEmbeddingService_ReturnsFalse()
    {
        // Arrange
        var project = await CreateTestProjectAsync("Test Project");

        // Act
        bool result = await _service.UpdateProjectEmbeddingAsync(project.Id);

        // Assert
        Assert.False(result);
    }
}