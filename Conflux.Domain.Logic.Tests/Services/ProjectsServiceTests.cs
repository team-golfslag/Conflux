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
        ProjectsService service = new(context);

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
            StartDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2023, 12, 31, 23, 59, 59, DateTimeKind.Utc),
        };

        context.Projects.Add(originalProject);
        await context.SaveChangesAsync();

        ProjectsService service = new(context);

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
        Project? reloaded = await context.Projects.FindAsync(originalProject.Id);
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
            StartDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2023, 12, 31, 23, 59, 59, DateTimeKind.Utc),
        };

        context.Projects.Add(originalProject);
        await context.SaveChangesAsync();

        ProjectsService service = new(context);

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
        Project? reloaded = await context.Projects.FindAsync(originalProject.Id);
        Assert.NotNull(reloaded);
        Assert.Equal("Patched Title", reloaded.Title);
        Assert.Equal("Patched Description", reloaded.Description);
        Assert.Equal(new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc), reloaded.StartDate);
        Assert.Equal(new DateTime(2024, 3, 1, 23, 59, 59, DateTimeKind.Utc), reloaded.EndDate);
    }
}
