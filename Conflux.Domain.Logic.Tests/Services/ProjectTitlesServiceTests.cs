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

        List<ProjectTitleResponseDTO> response = await _service.UpdateTitleAsync(project.Id, dto);

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

        Guid projectId = Guid.CreateVersion7();

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
                    Id = Guid.CreateVersion7(),
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
                    Id = Guid.CreateVersion7(),
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
                    Id = Guid.CreateVersion7(),
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
                    Id = Guid.CreateVersion7(),
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
                    Id = Guid.CreateVersion7(),
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

    [Fact]
    public async Task UpdateTitleAsync_ShouldThrow_WhenProjectDoesNotExist()
    {
        ProjectTitleRequestDTO dto = new()
        {
            Text = "New Title",
            Language = new()
            {
                Id = "eng",
            },
            Type = TitleType.Primary,
        };

        await Assert.ThrowsAsync<ProjectNotFoundException>(async () =>
            await _service.UpdateTitleAsync(Guid.Empty, dto));
    }

    [Fact]
    public async Task UpdateTitleAsync_ShouldUpdateExistingTitle_WhenCreatedToday()
    {
        // Setup project with titles
        Project project = await SetupDatabase();

        // Add a title created today
        ProjectTitle todayTitle = new()
        {
            Id = Guid.CreateVersion7(),
            ProjectId = project.Id,
            Text = "Today's title",
            Language = new()
            {
                Id = "eng",
            },
            Type = TitleType.Primary,
            StartDate = DateTime.UtcNow.AddDays(-1).Date,
            EndDate = null,
        };

        _context.ProjectTitles.Add(todayTitle);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Create update DTO
        ProjectTitleRequestDTO dto = new()
        {
            Text = "Updated Today's Title",
            Language = new()
            {
                Id = "fra",
            }, // Changed language
            Type = TitleType.Primary,
        };

        // Call update
        List<ProjectTitleResponseDTO> response = await _service.UpdateTitleAsync(project.Id, dto);

        Assert.Equal(7, response.Count);

        // Find the updated title
        ProjectTitleResponseDTO updatedTitle = response.Single(t => t.StartDate.Date == DateTime.UtcNow.Date);
        Assert.Equal(dto.Text, updatedTitle.Text);
        Assert.Equal(dto.Language.Id, updatedTitle.Language!.Id);
        Assert.Equal(DateTime.UtcNow.Date, updatedTitle.StartDate);
        Assert.Null(updatedTitle.EndDate);
    }

    [Fact]
    public async Task UpdateTitleAsync_ShouldEndCurrentAndCreateNew_WhenCurrentTitleIsOlder()
    {
        Project project = await SetupDatabase();

        // Find the current short title from the setup
        ProjectTitle oldTitle = await _context.ProjectTitles
            .SingleAsync(t => t.ProjectId == project.Id && t.Type == TitleType.Short && t.EndDate == null);

        // Create update DTO
        ProjectTitleRequestDTO dto = new()
        {
            Text = "New Short Title",
            Language = new()
            {
                Id = "eng",
            },
            Type = TitleType.Short,
        };

        // Call update
        List<ProjectTitleResponseDTO> response = await _service.UpdateTitleAsync(project.Id, dto);

        // Should have 6 titles now (5 original + 1 new)
        Assert.Equal(6, response.Count);

        // Verify old title was ended
        ProjectTitleResponseDTO endedTitle = response.Single(t => t.Id == oldTitle.Id);
        Assert.NotNull(endedTitle.EndDate);
        Assert.Equal(DateTime.UtcNow.Date, endedTitle.EndDate);

        // Verify new title was created
        ProjectTitleResponseDTO newTitle = response.Single(t => t.Type == TitleType.Short && t.EndDate == null);
        Assert.NotEqual(oldTitle.Id, newTitle.Id);
        Assert.Equal(dto.Text, newTitle.Text);
        Assert.Equal(dto.Language.Id, newTitle.Language.Id);
        Assert.Equal(DateTime.UtcNow.Date, newTitle.StartDate);
        Assert.Null(newTitle.EndDate);
    }

    [Fact]
    public async Task UpdateTitleAsync_ShouldMaintainMultipleTitleTypes()
    {
        Project project = await SetupDatabase();

        // Create new title of a different type
        ProjectTitleRequestDTO dto1 = new()
        {
            Text = "New Primary Title",
            Language = new()
            {
                Id = "eng",
            },
            Type = TitleType.Primary,
        };

        await _service.UpdateTitleAsync(project.Id, dto1);

        // Create new title of another type
        ProjectTitleRequestDTO dto2 = new()
        {
            Text = "New Short Title",
            Language = new()
            {
                Id = "eng",
            },
            Type = TitleType.Short,
        };

        List<ProjectTitleResponseDTO> response = await _service.UpdateTitleAsync(project.Id, dto2);

        // Should have 7 titles now (5 original + 2 new)
        Assert.Equal(7, response.Count);

        // Verify we have current titles of different types
        ProjectTitleResponseDTO currentPrimary = response.Single(t => t.Type == TitleType.Primary && t.EndDate == null);
        ProjectTitleResponseDTO currentShort = response.Single(t => t.Type == TitleType.Short && t.EndDate == null);

        Assert.Equal(dto1.Text, currentPrimary.Text);
        Assert.Equal(dto2.Text, currentShort.Text);
        Assert.NotEqual(currentPrimary.Id, currentShort.Id);
    }

    [Fact]
    public async Task EndTitleAsync_EndsTitle_IfNotPrimary()
    {
        Project project = await SetupDatabase();

        ProjectTitle oldShortTitle = project.Titles.First(t => t is { Type: TitleType.Short, EndDate: null });

        await _service.EndTitleAsync(project.Id, oldShortTitle.Id);

        ProjectTitle? newTitle = await _context.ProjectTitles.FindAsync(oldShortTitle.Id);

        Assert.NotNull(newTitle);
        Assert.NotNull(newTitle.EndDate);
        Assert.Equal(DateTime.UtcNow.Date, newTitle.EndDate);
    }


    [Fact]
    public async Task EndTitleAsync_ThrowsException_WhenTitleIsPrimary()
    {
        Project project = await SetupDatabase();

        ProjectTitle primaryTitle = project.Titles.First(t => t is { Type: TitleType.Primary, EndDate: null });

        await Assert.ThrowsAsync<CantEndPrimaryTitleException>(async () =>
            await _service.EndTitleAsync(project.Id, primaryTitle.Id));
    }

    [Fact]
    public async Task EndTitleAsync_ThrowsException_WhenTitleHasAlreadyEnded()
    {
        Project project = await SetupDatabase();

        ProjectTitle endedTitle = project.Titles.First(t => t is { Type: TitleType.Short, EndDate: not null });

        await Assert.ThrowsAsync<CantEndEndedTitleException>(async () =>
            await _service.EndTitleAsync(project.Id, endedTitle.Id));
    }

    [Fact]
    public async Task DeleteTitleAsync_ThrowsException_WhenOnlyPrimaryTitle()
    {
        Project project = new()
        {
            Id = Guid.CreateVersion7(),
            SCIMId = null,
            RAiDInfo = null,
            StartDate = DateTime.UtcNow.Date,
            EndDate = null,
            Users = null,
            Contributors = null,
            Products = null,
            Organisations = null,
            Titles =
            [
                new ProjectTitle
                {
                    Id = Guid.CreateVersion7(),
                    Text = "Test",
                    Language = new()
                    {
                        Id = "eng",
                    },
                    Type = TitleType.Primary,
                    StartDate = DateTime.UtcNow.Date,
                    EndDate = null,
                },
            ],
            Descriptions = null,
            LastestEdit = default,
        };

        _context.Add(project);
        await _context.SaveChangesAsync();

        await Assert.ThrowsAsync<CantDeleteTitleException>(() =>
            _service.DeleteTitleAsync(project.Id, project.Titles[0].Id));
    }

    [Fact]
    public async Task DeleteTitleAsync_ThrowsException_WhenTitleSucceeded()
    {
        Project project = new()
        {
            Id = Guid.CreateVersion7(),
            SCIMId = null,
            RAiDInfo = null,
            StartDate = DateTime.UtcNow.Date,
            EndDate = null,
            Users = null,
            Contributors = null,
            Products = null,
            Organisations = null,
            Titles =
            [
                new ProjectTitle
                {
                    Id = Guid.CreateVersion7(),
                    Text = "Test",
                    Language = new()
                    {
                        Id = "eng",
                    },
                    Type = TitleType.Primary,
                    StartDate = DateTime.UtcNow.Date,
                    EndDate = null,
                },
                new ProjectTitle
                {
                    Id = Guid.CreateVersion7(),
                    Text = "TeST",
                    Language = new()
                    {
                        Id = "nld",
                    },
                    Type = TitleType.Acronym,
                    StartDate = DateTime.UtcNow.Date.Subtract(TimeSpan.FromDays(-1)),
                    EndDate = DateTime.UtcNow.Date,
                },
                new ProjectTitle
                {
                    Id = Guid.CreateVersion7(),
                    Text = "T.E.S.T.",
                    Language = new()
                    {
                        Id = "nld",
                    },
                    Type = TitleType.Acronym,
                    StartDate = DateTime.UtcNow,
                    EndDate = null,
                },
            ],
            Descriptions = null,
            LastestEdit = default,
        };
        
        
        _context.Add(project);
        await _context.SaveChangesAsync();

        ProjectTitle title = project.Titles.First(t => t is { Type: TitleType.Acronym, EndDate: not null });

        await Assert.ThrowsAsync<CantDeleteTitleException>(() =>
            _service.DeleteTitleAsync(project.Id, title.Id));
    }

    [Fact]
    public async Task DeleteTitleAsync_Succeeds_WhenTitleCreatedToday()
    {
        Project project = new()
        {
            Id = Guid.CreateVersion7(),
            SCIMId = null,
            RAiDInfo = null,
            StartDate = DateTime.UtcNow.Date,
            EndDate = null,
            Users = null,
            Contributors = null,
            Products = null,
            Organisations = null,
            Titles =
            [
                new ProjectTitle
                {
                    Id = Guid.CreateVersion7(),
                    Text = "Test",
                    Language = new()
                    {
                        Id = "eng",
                    },
                    Type = TitleType.Primary,
                    StartDate = DateTime.UtcNow.Date,
                    EndDate = null,
                },
                new ProjectTitle
                {
                    Id = Guid.CreateVersion7(),
                    Text = "TeST",
                    Language = new()
                    {
                        Id = "nld",
                    },
                    Type = TitleType.Acronym,
                    StartDate = DateTime.UtcNow.Date.Subtract(TimeSpan.FromDays(-1)),
                    EndDate = DateTime.UtcNow.Date,
                },
                new ProjectTitle
                {
                    Id = Guid.CreateVersion7(),
                    Text = "T.E.S.T.",
                    Language = new()
                    {
                        Id = "nld",
                    },
                    Type = TitleType.Acronym,
                    StartDate = DateTime.UtcNow,
                    EndDate = null,
                },
            ],
            Descriptions = null,
            LastestEdit = default,
        };
        
        
        _context.Add(project);
        await _context.SaveChangesAsync();

        ProjectTitle title = project.Titles.First(t => t is { Type: TitleType.Acronym, EndDate: null });

        await _service.DeleteTitleAsync(project.Id, title.Id);
        
        Assert.Null(await _context.ProjectTitles.FindAsync(title.Id));
    }
}
