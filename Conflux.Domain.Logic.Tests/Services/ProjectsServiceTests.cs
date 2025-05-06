// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
using Conflux.Domain.Logic.DTOs;
using Conflux.Domain.Logic.DTOs.Patch;
using Conflux.Domain.Logic.Exceptions;
using Conflux.Domain.Logic.Services;
using Conflux.Integrations.RAiD;
using Microsoft.EntityFrameworkCore;
using RAiD.Net;
using Testcontainers.PostgreSql;
using Xunit;

namespace Conflux.Domain.Logic.Tests.Services;

public class ProjectsServiceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();
    private ConfluxContext _context = null!;
    private readonly IProjectMapperService _projectMapperService = null!;
    private readonly IRAiDService _raidService = null!;
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
        ProjectsService service = new(_context, _userSessionService, _projectMapperService, _raidService);

        // Act & Assert
        await Assert.ThrowsAsync<ProjectNotFoundException>(async () => await service.PutProjectAsync(Guid.NewGuid(),
            new()
            {
                Titles =
                [
                    new()
                    {
                        Text = "non-existent project",
                        Type = TitleType.Primary,
                        StartDate = new(2021,
                            1,
                            1,
                            0,
                            0,
                            0,
                            DateTimeKind.Utc),
                    },
                ],
                Descriptions =
                [
                    new()
                    {
                        Text = "Will not update",
                        Language = Language.ENGLISH,
                        Type = DescriptionType.Primary,
                    },
                ],
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(1),
                Id = Guid.NewGuid(),
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
        ProjectsService service = new(_context, _userSessionService, _projectMapperService, _raidService);

        Guid projectId = Guid.NewGuid();

        // Insert a test project
        Project originalProject = new()
        {
            Id = projectId,
            Titles =
            [
                new()
                {
                    ProjectId = projectId,
                    Text = "Original Title",
                    Type = TitleType.Primary,
                    StartDate = new(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                },
            ],
            Descriptions =
            [
                new()
                {
                    ProjectId = projectId,
                    Text = "Original Description",
                    Type = DescriptionType.Primary,
                    Language = Language.ENGLISH,
                },
            ],
            StartDate = new(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2023, 12, 31, 23, 59, 59, DateTimeKind.Utc),
        };

        _context.Projects.Add(originalProject);
        await _context.SaveChangesAsync();

        // Prepare Put DTO
        ProjectDTO putDto = new()
        {
            Titles =
            [
                new()
                {
                    Text = "Updated Title",
                    Type = TitleType.Primary,
                    StartDate = new(2021,
                        1,
                        1,
                        0,
                        0,
                        0,
                        DateTimeKind.Utc),
                },
            ],
            Descriptions =
            [
                new()
                {
                    Text = "Updated Description",
                    Type = DescriptionType.Primary,
                    Language = Language.ENGLISH,
                },
            ],
            StartDate = new(2024,
                2,
                1,
                0,
                0,
                0,
                DateTimeKind.Utc),
            EndDate = new(2024,
                3,
                1,
                23,
                59,
                59,
                DateTimeKind.Utc),
            Id = projectId,
        };

        // Act
        ProjectDTO updatedProject = await service.PutProjectAsync(originalProject.Id, putDto);

        // Assert
        Assert.NotNull(updatedProject);
        Assert.Single(updatedProject.Titles);
        Assert.Equal("Updated Title", updatedProject.Titles[0].Text);
        Assert.Single(updatedProject.Descriptions);
        Assert.Equal("Updated Description", updatedProject.Descriptions[0].Text);
        Assert.Equal(new(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc), updatedProject.StartDate);
        Assert.Equal(new DateTime(2024, 3, 1, 23, 59, 59, DateTimeKind.Utc), updatedProject.EndDate);

        // Double-check by re-querying from the database
        Project? reloaded = await _context.Projects.Include(p => p.Titles)
            .Include(p => p.Descriptions)
            .SingleOrDefaultAsync(p => p.Id == originalProject.Id);
        Assert.NotNull(reloaded);
        Assert.Single(reloaded.Titles);
        Assert.Equal("Updated Title", reloaded.Titles[0].Text);
        Assert.Single(reloaded.Descriptions);
        Assert.Equal("Updated Description", reloaded.Descriptions[0].Text);
        Assert.Equal(new(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc), reloaded.StartDate);
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
        ProjectsService service = new(_context, _userSessionService, _projectMapperService, _raidService);

        Guid projectId = Guid.NewGuid();

        // Insert a test project
        Project originalProject = new()
        {
            Id = projectId,
            Titles =
            [
                new()
                {
                    ProjectId = projectId,
                    Text = "Original Title",
                    Type = TitleType.Primary,
                    StartDate = new(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                },
            ],
            Descriptions =
            [
                new()
                {
                    ProjectId = projectId,
                    Text = "Original Description",
                    Type = DescriptionType.Primary,
                    Language = Language.ENGLISH,
                },
            ],
            StartDate = new(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2023, 12, 31, 23, 59, 59, DateTimeKind.Utc),
        };

        _context.Projects.Add(originalProject);
        await _context.SaveChangesAsync();


        // Prepare patch DTO
        ProjectPatchDTO patchDto = new()
        {
            Titles =
            [
                new()
                {
                    Text = "Patched Title",
                    Type = TitleType.Primary,
                    StartDate = new(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                },
            ],
            Descriptions =
            [
                new()
                {
                    Text = "Patched Description",
                    Type = DescriptionType.Primary,
                    Language = Language.ENGLISH,
                },
            ],
            StartDate = new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2024, 3, 1, 23, 59, 59, DateTimeKind.Utc),
        };

        // Act
        ProjectDTO patchedProject = await service.PatchProjectAsync(originalProject.Id, patchDto);

        // Assert
        Assert.NotNull(patchedProject);
        Assert.Single(patchedProject.Titles);
        Assert.Equal("Patched Title", patchedProject.Titles[0].Text);
        Assert.Single(patchedProject.Descriptions);
        Assert.Equal("Patched Description", patchedProject.Descriptions[0].Text);
        Assert.Equal(new(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc), patchedProject.StartDate);
        Assert.Equal(new DateTime(2024, 3, 1, 23, 59, 59, DateTimeKind.Utc), patchedProject.EndDate);

        // Double-check by re-querying from the database
        Project? reloaded = await _context.Projects
            .Include(p => p.Titles)
            .Include(p => p.Descriptions)
            .SingleOrDefaultAsync(p => p.Id == originalProject.Id);
        Assert.NotNull(reloaded);
        Assert.Single(reloaded.Titles);
        Assert.Equal("Patched Title", reloaded.Titles[0].Text);
        Assert.Single(reloaded.Descriptions);
        Assert.Equal("Patched Description", reloaded.Descriptions[0].Text);
        Assert.Equal(new(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc), reloaded.StartDate);
        Assert.Equal(new DateTime(2024, 3, 1, 23, 59, 59, DateTimeKind.Utc), reloaded.EndDate);
    }
}
