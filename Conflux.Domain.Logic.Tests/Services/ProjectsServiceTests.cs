// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
using Conflux.Domain.Logic.DTOs;
using Conflux.Domain.Logic.Exceptions;
using Conflux.Domain.Logic.Services;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace Conflux.Domain.Logic.Tests.Services;

public class ProjectsServiceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();
    private ConfluxContext _context = null!;
    private UserSessionService _userSessionService = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        var options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        ConfluxContext context = new(options);
        await context.Database.EnsureCreatedAsync();
        _context = context;
        _userSessionService = new(null!, null!, null!, null!);
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    /// <summary>
    /// Given a project service
    /// When UpdateProjectAsync is called with a non-existent project ID
    /// Then null should be returned
    /// </summary>
    [Fact]
    public async Task UpdateProjectAsync_ShouldReturnNull_WhenProjectDoesNotExist()
    {
        // Arrange
        ProjectsService service = new(_context, _userSessionService);

        // Act & Assert
        await Assert.ThrowsAsync<ProjectNotFoundException>(async () => await service.PutProjectAsync(Guid.NewGuid(),
            new()
            {
                Title = "Non-existent project",
                Description = "Will not update",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(1),
            }));
    }

    /// <summary>
    /// Given a project service
    /// When PutProjectAsync is called with an existing project ID
    /// Then the project should be updated
    /// </summary>
    [Fact]
    public async Task PutProjectAsync_ShouldUpdateExistingProject()
    {
        // Arrange
        ProjectsService service = new(_context, _userSessionService);

        // Insert a test project
        Project originalProject = new()
        {
            Id = Guid.NewGuid(),
            Title = "Original Title",
            Description = "Original Description",
            StartDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2023, 12, 31, 23, 59, 59, DateTimeKind.Utc),
        };

        _context.Projects.Add(originalProject);
        await _context.SaveChangesAsync();

        // Prepare Put DTO
        ProjectPutDTO putDto = new()
        {
            Title = "Updated Title",
            Description = "Updated Description",
            StartDate = new(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new(2024, 3, 1, 23, 59, 59, DateTimeKind.Utc),
        };

        // Act
        Project updatedProject = await service.PutProjectAsync(originalProject.Id, putDto);

        // Assert
        Assert.NotNull(updatedProject);
        Assert.Equal("Updated Title", updatedProject.Title);
        Assert.Equal("Updated Description", updatedProject.Description);
        Assert.Equal(new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc), updatedProject.StartDate);
        Assert.Equal(new DateTime(2024, 3, 1, 23, 59, 59, DateTimeKind.Utc), updatedProject.EndDate);

        // Double-check by re-querying from the database
        Project? reloaded = await _context.Projects.FindAsync(originalProject.Id);
        Assert.NotNull(reloaded);
        Assert.Equal("Updated Title", reloaded.Title);
        Assert.Equal("Updated Description", reloaded.Description);
        Assert.Equal(new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc), reloaded.StartDate);
        Assert.Equal(new DateTime(2024, 3, 1, 23, 59, 59, DateTimeKind.Utc), reloaded.EndDate);
    }

    /// <summary>
    /// Given a project service
    /// When PatchProjectAsync is called with an existing project ID
    /// Then the project should be patched
    /// </summary>
    [Fact]
    public async Task PatchProjectAsync_ShouldPatchExistingProject()
    {
        // Arrange
        ProjectsService service = new(_context, _userSessionService);


        // Insert a test project
        Project originalProject = new()
        {
            Id = Guid.NewGuid(),
            Title = "Original Title",
            Description = "Original Description",
            StartDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2023, 12, 31, 23, 59, 59, DateTimeKind.Utc),
        };

        _context.Projects.Add(originalProject);
        await _context.SaveChangesAsync();


        // Prepare patch DTO
        ProjectPatchDTO patchDto = new()
        {
            Title = "Patched Title",
            Description = "Patched Description",
            StartDate = new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2024, 3, 1, 23, 59, 59, DateTimeKind.Utc),
        };

        // Act
        Project patchedProject = await service.PatchProjectAsync(originalProject.Id, patchDto);

        // Assert
        Assert.NotNull(patchedProject);
        Assert.Equal("Patched Title", patchedProject.Title);
        Assert.Equal("Patched Description", patchedProject.Description);
        Assert.Equal(new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc), patchedProject.StartDate);
        Assert.Equal(new DateTime(2024, 3, 1, 23, 59, 59, DateTimeKind.Utc), patchedProject.EndDate);

        // Double-check by re-querying from the database
        Project? reloaded = await _context.Projects.FindAsync(originalProject.Id);
        Assert.NotNull(reloaded);
        Assert.Equal("Patched Title", reloaded.Title);
        Assert.Equal("Patched Description", reloaded.Description);
        Assert.Equal(new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc), reloaded.StartDate);
        Assert.Equal(new DateTime(2024, 3, 1, 23, 59, 59, DateTimeKind.Utc), reloaded.EndDate);
    }

    [Fact]
    public async Task AddPersonToProjectAsync_ShouldReturnProject_WhenProjectAndPersonExist()
    {
        // Arrange
        ProjectsService projectsService = new(_context, _userSessionService);


        Guid projectId = Guid.NewGuid();
        Guid personId = Guid.NewGuid();

        // Insert a test project
        Project testProject = new()
        {
            Id = projectId,
            Title = "Test Title",
            Description = "Test Description",
            StartDate = new(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new(2023, 12, 31, 0, 0, 0, DateTimeKind.Utc),
        };

        _context.Projects.Add(testProject);
        await _context.SaveChangesAsync();

        // Insert a test user
        User testUser = new()
        {
            Id = personId,
            Name = "Test User",
            SCIMId = "test-scim-id",
        };

        _context.Users.Add(testUser);
        await _context.SaveChangesAsync();

        // Act
        Project project = await projectsService.AddPersonToProjectAsync(projectId, personId);

        // Assert
        Assert.NotNull(project);
        Assert.Equal(projectId, project.Id);
        Assert.Equal(testProject.Title, project.Title);
        Assert.Equal(project.Users[0].Id, testUser.Id);
        Assert.Equal(project.Users[0].Name, testUser.Name);
    }

    [Fact]
    public async Task AddPersonToProjectAsync_ShouldThrow_WhenProjectDoesNotExist()
    {
        // Arrange
        ProjectsService projectsService = new(_context, _userSessionService);


        Guid projectId = Guid.NewGuid();
        Guid personId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ProjectNotFoundException>(() =>
            projectsService.AddPersonToProjectAsync(projectId, personId));
    }

    [Fact]
    public async Task AddPersonToProjectAsync_ShouldThrow_WhenPersonDoesNotExist()
    {
        // Arrange
        ProjectsService projectsService = new(_context, _userSessionService);


        Guid projectId = Guid.NewGuid();
        Guid personId = Guid.NewGuid();

        // Insert a test project
        Project testProject = new()
        {
            Id = projectId,
            Title = "Test Title",
            Description = "Test Description",
            StartDate = new(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new(2023, 12, 31, 0, 0, 0, DateTimeKind.Utc),
        };

        _context.Projects.Add(testProject);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<PersonNotFoundException>(() =>
            projectsService.AddPersonToProjectAsync(projectId, personId));
    }

    [Fact]
    public async Task AddPersonToProjectAsync_ShouldReturnProject_WhenPersonAlreadyExists()
    {
        // Arrange
        ProjectsService projectsService = new(_context, _userSessionService);

        Guid projectId = Guid.NewGuid();
        Guid personId = Guid.NewGuid();

        // Insert a test project
        Project testProject = new()
        {
            Id = projectId,
            Title = "Test Title",
        };

        _context.Projects.Add(testProject);

        // Insert a test user
        User testUser = new()
        {
            Id = personId,
            Name = "Test User",
            SCIMId = "test-scim-id",
        };

        _context.Users.Add(testUser);
        await _context.SaveChangesAsync();

        // Act
        await projectsService.AddPersonToProjectAsync(projectId, personId);

        // Assert
        await Assert.ThrowsAsync<PersonAlreadyAddedToProjectException>(() =>
            projectsService.AddPersonToProjectAsync(projectId, personId));
    }

    [Fact]
    public async Task AddPersonToProject_ShouldReturnProject_WhenSuccessful()
    {
        // Arrange
        ProjectsService projectsService = new(_context, _userSessionService);


        Guid projectId = Guid.NewGuid();
        Guid personId = Guid.NewGuid();

        // Create a test project
        Project testProject = new()
        {
            Id = projectId,
            Title = "Test Title",
            Description = "Test Description",
            StartDate = new(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new(2023, 12, 31, 0, 0, 0, DateTimeKind.Utc),
        };

        _context.Projects.Add(testProject);

        // Insert a test user
        User testUser = new()
        {
            Id = personId,
            Name = "Test User",
            SCIMId = "test-scim-id",
        };

        _context.Users.Add(testUser);
        await _context.SaveChangesAsync();

        // Act
        Project project = await projectsService.AddPersonToProjectAsync(projectId, testUser.Id);

        // Assert
        Assert.NotNull(project);
        Assert.Equal(project.Users[0].Id, testUser.Id);
        Assert.Equal(project.Users[0].Name, testUser.Name);
    }

    [Fact]
    public async Task AddPersonToProject_ShouldReturnNotFound_WhenProjectDoesNotExist()
    {
        // Arrange
        ProjectsService projectsService = new(_context, _userSessionService);


        Guid projectId = Guid.NewGuid();
        Guid personId = Guid.NewGuid();

        // Insert a test user
        User testUser = new()
        {
            Id = personId,
            Name = "Test User",
            SCIMId = "test-scim-id",
        };

        _context.Users.Add(testUser);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ProjectNotFoundException>(() =>
            projectsService.AddPersonToProjectAsync(projectId, personId));
    }

    [Fact]
    public async Task AddPersonToProject_ShouldReturnBadRequest_WhenPersonAlreadyAdded()
    {
        // Arrange
        ProjectsService projectsService = new(_context, _userSessionService);

        Guid projectId = Guid.NewGuid();
        Guid personId = Guid.NewGuid();

        // Create a test project
        Project testProjectDto = new()
        {
            Id = projectId,
            Title = "Test Title",
            Description = "Test Description",
            StartDate = new(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new(2023, 12, 31, 0, 0, 0, DateTimeKind.Utc),
        };

        _context.Projects.Add(testProjectDto);

        // Insert a test user
        User testUser = new()
        {
            Id = personId,
            Name = "Test User",
            SCIMId = "test-scim-id",
        };

        _context.Users.Add(testUser);
        await _context.SaveChangesAsync();

        // Act & Assert
        Project resultProject = await projectsService.AddPersonToProjectAsync(projectId, personId);
        Assert.NotNull(resultProject);

        await Assert.ThrowsAsync<PersonAlreadyAddedToProjectException>(() =>
            projectsService.AddPersonToProjectAsync(projectId, personId));
    }
}
