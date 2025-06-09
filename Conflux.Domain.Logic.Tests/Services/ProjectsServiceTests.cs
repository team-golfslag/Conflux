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
using Moq;
using Testcontainers.PostgreSql;
using Xunit;

namespace Conflux.Domain.Logic.Tests.Services;

public class ProjectsServiceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();
    private readonly Mock<IUserSessionService> _userSessionServiceMock = new();
    private ConfluxContext _context = null!;
    private ProjectsService _service = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        DbContextOptions<ConfluxContext> options =
            new DbContextOptionsBuilder<ConfluxContext>()
                .UseNpgsql(_postgres.GetConnectionString())
                .Options;

        _context = new ConfluxContext(options);
        await _context.Database.EnsureCreatedAsync();

        _service = new ProjectsService(_context, _userSessionServiceMock.Object);
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
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
            Id = Guid.NewGuid(),
            Name = "Test User",
            Email = email
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            SRAMId = $"sram-id-{Guid.NewGuid()}",
            SCIMId = $"scim-id-{Guid.NewGuid()}",
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
        var projectId = Guid.NewGuid();
        var project = new Project
        {
            Id = projectId,
            SCIMId = $"project-scim-{Guid.NewGuid()}",
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
            Id = Guid.NewGuid(),
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
            User = user
        };
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
                    Guid.NewGuid(),
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

        // Manually add relations not covered by helpers
        project.Organisations.Add(
            new ProjectOrganisation
            {
                OrganisationId = organisation.Id,
                ProjectId = project.Id
            }
        );
        project.Products.Add(
            new Product
            {
                Id = Guid.NewGuid(),
                Title = "Test Product",
                Url = "https://example.com/product",
                Type = ProductType.Software,
            }
        );
        await _context.SaveChangesAsync();

        var queryDto = new ProjectQueryDTO { Query = "Test", };

        // Act
        string csvContent = await _service.ExportProjectsToCsvAsync(queryDto);

        // Assert
        Assert.NotNull(csvContent);
        Assert.NotEmpty(csvContent);
        Assert.Contains("Id", csvContent);
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
            s => s.CommitUser(It.IsAny<UserSession>()),
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
            s => s.CommitUser(It.IsAny<UserSession>()),
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
            () => _service.FavoriteProjectAsync(Guid.NewGuid(), true)
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
            () => _service.FavoriteProjectAsync(Guid.NewGuid(), true)
        );
    }

    [Fact]
    public async Task FavoriteProjectAsync_ShouldNotAddDuplicates_WhenProjectAlreadyFavorited()
    {
        // Arrange
        User user = await CreateTestUserAsync();
        Project project = await CreateTestProjectAsync(users: [user]);
        user.FavoriteProjectIds.Add(project.Id); // Pre-favorite the project
        await _context.SaveChangesAsync();
        SetupUserSessionMock(user);

        // Act
        await _service.FavoriteProjectAsync(project.Id, true);

        // Assert
        User? updatedUser = await _context.Users.FindAsync(user.Id);
        Assert.NotNull(updatedUser);
        // The test name implies duplicates should not be added.
        // The list should still contain only one entry for this project.
        Assert.Single(updatedUser.FavoriteProjectIds);
        Assert.Equal(project.Id, updatedUser.FavoriteProjectIds.First());
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
            s => s.CommitUser(It.IsAny<UserSession>()),
            Times.Once
        );
    }
}