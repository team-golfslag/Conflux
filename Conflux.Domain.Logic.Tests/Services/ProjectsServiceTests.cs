// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
using Conflux.Domain.Logic.DTOs.Requests;
using Conflux.Domain.Logic.DTOs.Responses;
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
    private readonly IProjectMapperService _projectMapperService = null!;
    private readonly IRAiDService _raidService = null!;
    private ConfluxContext _context = null!;
    private UserSessionService _userSessionService = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        DbContextOptions<ConfluxContext> options = new DbContextOptionsBuilder<ConfluxContext>()
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
        ProjectRequestDTO putRequestDTO = new()
        {
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
        };

        // Act
        ProjectResponseDTO updatedProject = await service.PutProjectAsync(originalProject.Id, putRequestDTO);

        // Assert
        Assert.NotNull(updatedProject);
        Assert.Equal(new(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc), updatedProject.StartDate);
        Assert.Equal(new DateTime(2024, 3, 1, 23, 59, 59, DateTimeKind.Utc), updatedProject.EndDate);

        // Double-check by re-querying from the database
        Project? reloaded = await _context.Projects.Include(p => p.Titles)
            .Include(p => p.Descriptions)
            .SingleOrDefaultAsync(p => p.Id == originalProject.Id);
        Assert.NotNull(reloaded);
        Assert.Equal(new(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc), reloaded.StartDate);
        Assert.Equal(new DateTime(2024, 3, 1, 23, 59, 59, DateTimeKind.Utc), reloaded.EndDate);
    }
}
