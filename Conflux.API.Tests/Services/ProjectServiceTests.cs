using Conflux.API.DTOs;
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

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }
    
    /// <summary>
    /// Given a project service
    /// When GetProjectByIdAsync is called with an existing project ID
    /// The the project should be returned
    /// </summary>
    [Fact]
    public async Task GetProjectByIdAsync_ShouldReturnProject_WhenProjectExists()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using ConfluxContext context = new(options);
        await context.Database.EnsureCreatedAsync();
        
        ProjectService projectService = new(context);
        
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

        context.Projects.Add(testProject);
        await context.SaveChangesAsync();
        
        // Act
        Project? project = await projectService.GetProjectByIdAsync(projectId);
        
        // Assert
        Assert.NotNull(project);
        Assert.Equal(projectId, project.Id);
        Assert.Equal(testProject.Title, project.Title);
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
        var options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using ConfluxContext context = new(options);
        await context.Database.EnsureCreatedAsync();

        // No project is added to the database, so it definitely doesn't exist
        ProjectService service = new(context);

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
        var options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using ConfluxContext context = new(options);
        await context.Database.EnsureCreatedAsync();

        // Insert a test project
        Project originalProject = new()
        {
            Id = Guid.NewGuid(),
            Title = "Original Title",
            Description = "Original Description",
            StartDate = new DateOnly(2023, 1, 1),
            EndDate = new DateOnly(2023, 12, 31)
        };

        context.Projects.Add(originalProject);
        await context.SaveChangesAsync();

        ProjectService service = new(context);

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
        Assert.Equal("Updated Title", updatedProject!.Title);
        Assert.Equal("Updated Description", updatedProject.Description);
        Assert.Equal(new DateOnly(2024, 2, 1), updatedProject.StartDate);
        Assert.Equal(new DateOnly(2024, 3, 1), updatedProject.EndDate);

        // Double-check by re-querying from the database
        Project? reloaded = await context.Projects.FindAsync(originalProject.Id);
        Assert.NotNull(reloaded);
        Assert.Equal("Updated Title", reloaded!.Title);
        Assert.Equal("Updated Description", reloaded.Description);
        Assert.Equal(new DateOnly(2024, 2, 1), reloaded.StartDate);
        Assert.Equal(new DateOnly(2024, 3, 1), reloaded.EndDate);
    }
}