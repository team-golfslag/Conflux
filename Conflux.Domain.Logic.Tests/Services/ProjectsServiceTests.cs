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
using Conflux.Integrations.RAiD;
using Microsoft.EntityFrameworkCore;
using Moq;
using RAiD.Net;
using Testcontainers.PostgreSql;
using Xunit;

namespace Conflux.Domain.Logic.Tests.Services;

public class ProjectsServiceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();
    private readonly Mock<IUserSessionService> _userSessionServiceMock = new();
    private ConfluxContext _context = null!;
    private ProjectsService _service;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        DbContextOptions<ConfluxContext> options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        ConfluxContext context = new(options);
        await context.Database.EnsureCreatedAsync();
        _context = context;
        _userSessionServiceMock.Setup(s => s.GetUser())
            .ReturnsAsync(UserSession.Development);
        _service = new(_context, _userSessionServiceMock.Object);
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
        // Act & Assert
        await Assert.ThrowsAsync<ProjectNotFoundException>(async () => await _service.PutProjectAsync(Guid.NewGuid(),
            new()
            {
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(1),
            }));
    }

    [Fact]
    public async Task ExportProjectsToCsvAsync_ShouldReturnCsvContent()
    {
        // Arrange
        // insert a test person
        Guid personId = Guid.NewGuid();
        Person person = new()
        {
            Id = personId,
            Name = "Test User",
        };

        // insert a test organisation
        Guid organisationId = Guid.NewGuid();
        Organisation organisation = new()
        {
            Id = organisationId,
            RORId = "https://ror.org/00x00x00",
            Name = "Test Organisation",
        };

        // Insert a test project
        Guid projectId = Guid.NewGuid();
        Project project1 = new()
        {
            Id = projectId,
            SCIMId = "SCIM",
            Titles =
            [
                new()
                {
                    ProjectId = projectId,
                    Text = "Test Project",
                    Type = TitleType.Primary,
                    StartDate = new(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                },
            ],
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            Organisations =
            [
                new()
                {
                    OrganisationId = organisationId,
                    ProjectId = projectId,
                },
            ],
            Contributors =
            [
                new()
                {
                    PersonId = personId,
                    ProjectId = projectId,
                },
            ],
            Products =
            [
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Test Product",
                    Url = "https://example.com/product",
                    Type = ProductType.Software,
                },
            ],
        };

        _context.People.Add(person);
        _context.Organisations.Add(organisation);
        _context.Projects.Add(project1);
        await _context.SaveChangesAsync();

        ProjectQueryDTO queryDto = new()
        {
            Query = "Test",
            // StartDate = DateTime.UtcNow.AddDays(-30),
            // EndDate = DateTime.UtcNow.AddDays(30),
        };

        // Act
        string csvContent = await _service.ExportProjectsToCsvAsync(queryDto);

        // Assert
        Assert.NotNull(csvContent);
        Assert.NotEmpty(csvContent);
        Assert.Contains("Id", csvContent);
        Assert.Contains(projectId.ToString(), csvContent);
        Assert.Contains("Test User", csvContent);
        Assert.Contains("Test Organisation", csvContent);
        Assert.Contains("Test Product", csvContent);
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
        ProjectResponseDTO updatedProject = await _service.PutProjectAsync(originalProject.Id, putRequestDTO);

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
