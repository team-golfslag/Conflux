using Conflux.API.DTOs;
using Conflux.API.Results;
using Conflux.API.Services;
using Conflux.Data;
using Conflux.Domain;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace Conflux.API.Tests.Services;

public class ProjectServiceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();
    private ConfluxContext _context;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        var options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
        
        ConfluxContext context = new(options);
        await context.Database.EnsureCreatedAsync();
        _context = context;
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }
    
    /// <summary>
    /// Given a project service
    /// When GetProjectByIdAsync is called with an existing project ID
    /// The project should be returned
    /// </summary>
    [Fact]
    public async Task GetProjectByIdAsync_ShouldReturnProject_WhenProjectExists()
    {
        // Arrange
        ProjectService projectService = new(_context);
        
        Guid projectId = Guid.NewGuid();
        
        // Insert a test project
        Project testProject = new()
        {
            Id = projectId,
            Title = "Test Title",
            Description = "Test Description",
            StartDate = new DateOnly(2023, 1, 1),
            EndDate = new DateOnly(2023, 12, 31)
        };

        _context.Projects.Add(testProject);
        await _context.SaveChangesAsync();
        
        // Act
        Project? project = await projectService.GetProjectByIdAsync(projectId);
        
        // Assert
        Assert.NotNull(project);
        Assert.Equal(projectId, project.Id);
        Assert.Equal(testProject.Title, project.Title);
    }

    /// <summary>
    /// Given a project service
    /// When GetProjectByIdAsync is called with a non-existing project ID
    /// Then null should be returned
    /// </summary>
    [Fact]
    public async Task GetProjectByIdAsync_ShouldReturnNull_WhenProjectDoesNotExist()
    {
        // Arrange
        ProjectService projectService = new(_context);
        
        Guid projectId = Guid.NewGuid();
        
        // Act
        Project? project = await projectService.GetProjectByIdAsync(projectId);
        
        // Assert
        Assert.Null(project);
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
        // No project is added to the database, so it definitely doesn't exist
        ProjectService service = new(_context);

        // Act
        Project? result = await service.UpdateProjectAsync(Guid.NewGuid(), new()
        {
            Title = "Non-existent project",
            Description = "Will not update",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = null,
        });

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Given a project service
    /// When UpdateProjectAsync is called with an existing project ID
    /// Then the project should be updated
    /// </summary>
    [Fact]
    public async Task UpdateProjectAsync_ShouldUpdateExistingProject()
    {
        // Arrange
        // Insert a test project
        Project originalProject = new()
        {
            Id = Guid.NewGuid(),
            Title = "Original Title",
            Description = "Original Description",
            StartDate = new DateOnly(2023, 1, 1),
            EndDate = new DateOnly(2023, 12, 31)
        };

        _context.Projects.Add(originalProject);
        await _context.SaveChangesAsync();

        ProjectService service = new(_context);

        // Prepare update DTO
        ProjectUpdateDto updateDto = new()
        {
            Title = "Updated Title",
            Description = "Updated Description",
            StartDate = new DateOnly(2024, 2, 1),
            EndDate = new DateOnly(2024, 3, 1)
        };

        // Act
        Project? updatedProject = await service.UpdateProjectAsync(originalProject.Id, updateDto);

        // Assert
        Assert.NotNull(updatedProject);
        Assert.Equal("Updated Title", updatedProject.Title);
        Assert.Equal("Updated Description", updatedProject.Description);
        Assert.Equal(new DateOnly(2024, 2, 1), updatedProject.StartDate);
        Assert.Equal(new DateOnly(2024, 3, 1), updatedProject.EndDate);

        // Double-check by re-querying from the database
        Project? reloaded = await _context.Projects.FindAsync(originalProject.Id);
        Assert.NotNull(reloaded);
        Assert.Equal("Updated Title", reloaded.Title);
        Assert.Equal("Updated Description", reloaded.Description);
        Assert.Equal(new DateOnly(2024, 2, 1), reloaded.StartDate);
        Assert.Equal(new DateOnly(2024, 3, 1), reloaded.EndDate);
    }

    [Fact]
    public async Task AddPersonToProjectAsync_ShouldReturnProject_WhenProjectAndPersonExist()
    {
        // Arrange
        ProjectService projectService = new(_context);
        
        Guid projectId = Guid.NewGuid();
        Guid personId = Guid.NewGuid();
        
        // Insert a test project
        Project testProject = new()
        {
            Id = projectId,
            Title = "Test Title",
            Description = "Test Description",
            StartDate = new DateOnly(2023, 1, 1),
            EndDate = new DateOnly(2023, 12, 31)
        };
        
        _context.Projects.Add(testProject);
        await _context.SaveChangesAsync();
        
        // Insert a test person
        Person testPerson = new()
        {
            Id = personId,
            Name = "Test Person",
        };
        
        _context.People.Add(testPerson);
        await _context.SaveChangesAsync();
        
        // Act
        ProjectResult projectResult = await projectService.AddPersonToProjectAsync(projectId, personId);
        
        // Assert
        Assert.NotNull(projectResult);
        Assert.NotNull(projectResult.Project);
        Assert.Equal(projectId, projectResult.Project.Id);
        Assert.Equal(testProject.Title, projectResult.Project.Title);
        Assert.Equal(ProjectResultType.Success, projectResult.ProjectResultType);
        Assert.Equal(projectResult.Project.People[0].Id, testPerson.Id);
        Assert.Equal(projectResult.Project.People[0].Name, testPerson.Name);
    }

    [Fact]
    public async Task AddPersonToProjectAsync_ShouldReturnNull_WhenProjectDoesNotExist()
    {
        // Arrange
        ProjectService projectService = new(_context);
        
        Guid projectId = Guid.NewGuid();
        Guid personId = Guid.NewGuid();
        
        // Act
        ProjectResult projectResult = await projectService.AddPersonToProjectAsync(projectId, personId);
        
        // Assert
        Assert.NotNull(projectResult);
        Assert.Null(projectResult.Project);
        Assert.Equal(ProjectResultType.ProjectNotFound, projectResult.ProjectResultType);
    }

    [Fact]
    public async Task AddPersonToProjectAsync_ShouldReturnProject_WhenPersonDoesNotExist()
    {
        // Arrange
        ProjectService projectService = new(_context);
        
        Guid projectId = Guid.NewGuid();
        Guid personId = Guid.NewGuid();
        
        // Insert a test project
        Project testProject = new()
        {
            Id = projectId,
            Title = "Test Title",
            Description = "Test Description",
            StartDate = new DateOnly(2023, 1, 1),
            EndDate = new DateOnly(2023, 12, 31)
        };
        
        _context.Projects.Add(testProject);
        await _context.SaveChangesAsync();
        
        // Act
        ProjectResult projectResult = await projectService.AddPersonToProjectAsync(projectId, personId);
        
        // Assert
        Assert.NotNull(projectResult);
        Assert.NotNull(projectResult.Project);
        Assert.Equal(projectId, projectResult.Project.Id);
        Assert.Equal(testProject.Title, projectResult.Project.Title);
        Assert.Equal(ProjectResultType.PersonNotFound, projectResult.ProjectResultType);
        Assert.Empty(projectResult.Project.People);
    }

    [Fact]
    public async Task AddPersonToProjectAsync_ShouldReturnProject_WhenPersonAlreadyExists()
    {
        // Arrange
        ProjectService projectService = new(_context);
        
        Guid projectId = Guid.NewGuid();
        Guid personId = Guid.NewGuid();
        
        // Insert a test project
        Project testProject = new()
        {
            Id = projectId,
            Title = "Test Title",
        };
        
        _context.Projects.Add(testProject);
                
        // Insert a test person
        Person testPerson = new()
        {
            Id = personId,
            Name = "Test Person",
        };
                
        _context.People.Add(testPerson);
        await _context.SaveChangesAsync();
        
        await projectService.AddPersonToProjectAsync(projectId, personId);
         
        // Act
        ProjectResult projectResult = await projectService.AddPersonToProjectAsync(projectId, personId);

        // Assert
        Assert.NotNull(projectResult);
        Assert.NotNull(projectResult.Project);
        Assert.Equal(projectId, projectResult.Project.Id);
        Assert.Equal(testProject.Title, projectResult.Project.Title);
        Assert.NotNull(projectResult.Project.People[0]);
        Assert.Equal(ProjectResultType.PersonAlreadyAdded, projectResult.ProjectResultType);
    }
}