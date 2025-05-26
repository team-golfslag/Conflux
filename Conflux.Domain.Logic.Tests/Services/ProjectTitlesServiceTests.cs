// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
using Conflux.Domain.Logic.DTOs.Requests;
using Conflux.Domain.Logic.DTOs.Responses;
using Conflux.Domain.Logic.Exceptions;
using Conflux.Domain.Logic.Services;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace Conflux.Domain.Logic.Tests.Services;

public class ProjectTitlesServiceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();
    private ConfluxContext _context = null!;
    private ProjectTitlesService _service = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        DbContextOptions<ConfluxContext> options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        ConfluxContext context = new(options);
        await context.Database.EnsureCreatedAsync();
        _context = context;
        _service = new(_context);
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task GetTitlesByProjectIdAsync_ShouldThrow_WhenProjectDoesNotExist()
    {
        await Assert.ThrowsAsync<ProjectNotFoundException>(async () =>
        {
            await _service.GetTitlesByProjectIdAsync(Guid.Empty);
        });
    }

    [Fact]
    public async Task GetTitlesByProjectIdAsync_ShouldReturnAllTitles()
    {
        Project project = await SetupDatabase();

        List<ProjectTitleResponseDTO> response = await _service.GetTitlesByProjectIdAsync(project.Id);

        Assert.Equal(5, response.Count);
        Assert.Contains(response, r => r.Id == project.Titles[0].Id);
        Assert.Contains(response, r => r.Id == project.Titles[1].Id);
        Assert.Contains(response, r => r.Id == project.Titles[2].Id);
        Assert.Contains(response, r => r.Id == project.Titles[3].Id);
        Assert.Contains(response, r => r.Id == project.Titles[4].Id);
    }


    [Fact]
    public async Task GetCurrentTitleByTitleType_ShouldReturnNull_WhenNoTitleOfType()
    {
        Project project = await SetupDatabase();

        ProjectTitleResponseDTO? result = await _service.GetCurrentTitleByTitleType(project.Id, TitleType.Acronym);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetCurrentTitleByTitleType_ShouldReturnNull_WhenTitleExpired()
    {
        Project project = await SetupDatabase();

        ProjectTitleResponseDTO? result = await _service.GetCurrentTitleByTitleType(project.Id, TitleType.Alternative);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetCurrentTitleByTitleType_ShouldReturnLatestTitle_WhenNotExpired()
    {
        Project project = await SetupDatabase();

        ProjectTitleResponseDTO? result = await _service.GetCurrentTitleByTitleType(project.Id, TitleType.Short);

        Assert.NotNull(result);
        Assert.Equal("Test short title 2", result.Text);
    }

    [Fact]
    public async Task GetCurrentTitleByTitleType_ShouldThrow_WhenProjectDoesNotExist()
    {
        await Assert.ThrowsAsync<ProjectNotFoundException>(async () =>
            await _service.GetCurrentTitleByTitleType(Guid.Empty, TitleType.Primary));
    }

    [Fact]
    public async Task GetTitleByIdAsync_ShouldThrow_WhenProjectDoesNotExist()
    {
        Project project = await SetupDatabase();

        await Assert.ThrowsAsync<ProjectNotFoundException>(async () =>
            await _service.GetTitleByIdAsync(Guid.Empty, project.Titles[0].Id));
    }

    [Fact]
    public async Task GetTitleByIdAsync_ShouldThrow_WhenTitleDoesNotExist()
    {
        Project project = await SetupDatabase();

        await Assert.ThrowsAsync<ProjectTitleNotFoundException>(async () =>
            await _service.GetTitleByIdAsync(project.Id, Guid.Empty));
    }

    [Fact]
    public async Task GetTitleByIdAsync_ShouldReturnTitle()
    {
        Project project = await SetupDatabase();

        ProjectTitleResponseDTO response = await _service.GetTitleByIdAsync(project.Id, project.Titles[0].Id);

        Assert.Equal(project.Titles[0].Id, response.Id);
    }

    [Fact]
    public async Task CreateTitleAsync_ShouldCreateNewTitle_WhenNoOldTitle()
    {
        Project project = await SetupDatabase();

        ProjectTitleRequestDTO dto = new()
        {
            Text = "T.E.S.T.",
            Language = new()
            {
                Id = "eng",
            },
            Type = TitleType.Acronym,
        };

        List<ProjectTitleResponseDTO> response = await _service.CreateTitleAsync(project.Id, dto);

        Assert.Equal(6, response.Count);
        foreach (ProjectTitle originalTitle in project.Titles)
        {
            // Check that all old titles are intact
            ProjectTitleResponseDTO? title = response.Find(p => p.Id == originalTitle.Id);
            Assert.NotNull(title);
            Assert.Equal(originalTitle.EndDate, title.EndDate);
            Assert.Equal(originalTitle.Language, title.Language);
            Assert.Equal(originalTitle.StartDate, originalTitle.StartDate);
            Assert.Equal(originalTitle.Text, title.Text);
            Assert.Equal(originalTitle.ProjectId, title.ProjectId);
            Assert.Equal(originalTitle.Type, title.Type);

            response.RemoveAll(t => t.Id == originalTitle.Id);
        }

        ProjectTitleResponseDTO newTitle = response[0];
        Assert.Equal(dto.Text, newTitle.Text);
        Assert.Equal(dto.Language, newTitle.Language);
        Assert.Equal(dto.Type, newTitle.Type);
    }


    private async Task<Project> SetupDatabase()
    {
        _context.Projects.RemoveRange(_context.Projects);
        _context.ProjectTitles.RemoveRange(_context.ProjectTitles);

        Guid projectId = Guid.NewGuid();

        Project project = new()
        {
            Id = projectId,
            StartDate = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = null,
            Users = [],
            Contributors = [],
            Products = [],
            Organisations = [],
            Titles =
            [
                new()
                {
                    Id = Guid.NewGuid(),
                    ProjectId = projectId,
                    Text = "Test primary title 1",
                    Language = new()
                    {
                        Id = "eng",
                    },
                    Type = TitleType.Primary,
                    StartDate = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    EndDate = null,
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    ProjectId = projectId,
                    Text = "Test other expired title 1",
                    Language = new()
                    {
                        Id = "nld",
                    },
                    Type = TitleType.Alternative,
                    StartDate = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    EndDate = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    ProjectId = projectId,
                    Text = "Test other expired title 2",
                    Language = new()
                    {
                        Id = "eng",
                    },
                    Type = TitleType.Alternative,
                    StartDate = new(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    EndDate = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    ProjectId = projectId,
                    Text = "Test short expired title 1",
                    Language = new()
                    {
                        Id = "nld",
                    },
                    Type = TitleType.Short,
                    StartDate = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    EndDate = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    ProjectId = projectId,
                    Text = "Test short title 2",
                    Language = new()
                    {
                        Id = "eng",
                    },
                    Type = TitleType.Short,
                    StartDate = new(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    EndDate = null,
                },
            ],
            Descriptions = [],
            LastestEdit = DateTime.UtcNow,
        };

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        return project;
    }
}
