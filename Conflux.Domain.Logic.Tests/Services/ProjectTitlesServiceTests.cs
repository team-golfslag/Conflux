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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using Moq;
using Xunit;

namespace Conflux.Domain.Logic.Tests.Services;

public class ProjectTitlesServiceTests : IDisposable
{
    private readonly ConfluxContext _context;
    private readonly ProjectTitlesService _service;

    public ProjectTitlesServiceTests()
    {
        ServiceProvider serviceProvider = new ServiceCollection()
            .AddEntityFrameworkInMemoryDatabase()
            .BuildServiceProvider();

        DbContextOptions<ConfluxContext> options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.CreateVersion7()}")
            .UseInternalServiceProvider(serviceProvider)
            .Options;

        ConfluxContext context = new(options);
        context.Database.EnsureCreated();
        _context = context;
        
        // Create mocks for dependencies
        var mockProjectsService = new Mock<IProjectsService>();
        var mockLogger = new Mock<ILogger<ProjectTitlesService>>();
        
        _service = new(_context, mockProjectsService.Object, mockLogger.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
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
        // Arrange
        Guid nonExistentProjectId = Guid.CreateVersion7();
        Guid titleId = Guid.CreateVersion7();

        // Act & Assert
        await Assert.ThrowsAsync<ProjectNotFoundException>(async () =>
            await _service.GetTitleByIdAsync(nonExistentProjectId, titleId));
    }

    [Fact]
    public async Task GetTitleByIdAsync_ShouldThrow_WhenTitleDoesNotExist()
    {
        // Arrange
        Project project = await SetupDatabase();
        Guid nonExistentTitleId = Guid.CreateVersion7();

        // Act & Assert
        await Assert.ThrowsAsync<ProjectTitleNotFoundException>(async () =>
            await _service.GetTitleByIdAsync(project.Id, nonExistentTitleId));
    }

    [Fact]
    public async Task GetTitleByIdAsync_ShouldReturnTitle_WhenTitleExists()
    {
        // Arrange
        Project project = await SetupDatabase();
        ProjectTitle expectedTitle = project.Titles[0];

        // Act
        ProjectTitleResponseDTO result = await _service.GetTitleByIdAsync(project.Id, expectedTitle.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedTitle.Id, result.Id);
        Assert.Equal(expectedTitle.Text, result.Text);
        Assert.Equal(expectedTitle.Type, result.Type);
        Assert.Equal(expectedTitle.Language, result.Language);
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

    [Fact]
    public async Task DeleteTitleAsync_ShouldThrow_WhenProjectDoesNotExist()
    {
        // Arrange
        Guid nonExistentProjectId = Guid.CreateVersion7();
        Guid titleId = Guid.CreateVersion7();

        // Act & Assert
        await Assert.ThrowsAsync<ProjectNotFoundException>(async () =>
            await _service.DeleteTitleAsync(nonExistentProjectId, titleId));
    }

    [Fact]
    public async Task DeleteTitleAsync_ShouldThrow_WhenTitleDoesNotExist()
    {
        // Arrange
        Project project = await SetupDatabase();
        Guid nonExistentTitleId = Guid.CreateVersion7();

        // Act & Assert
        await Assert.ThrowsAsync<ProjectTitleNotFoundException>(async () =>
            await _service.DeleteTitleAsync(project.Id, nonExistentTitleId));
    }

    [Fact]
    public async Task DeleteTitleAsync_ShouldThrow_WhenTitleIsOlderThanYesterday()
    {
        // Arrange
        Project project = await SetupDatabase();
        ProjectTitle oldTitle = project.Titles.First(t => t.StartDate < DateTime.UtcNow.Date.AddDays(-1));

        // Act & Assert
        await Assert.ThrowsAsync<CantDeleteTitleException>(async () =>
            await _service.DeleteTitleAsync(project.Id, oldTitle.Id));
    }

    [Fact]
    public async Task DeleteTitleAsync_ShouldRestorePreviousTitle_WhenDeletingRecentTitle()
    {
        // Arrange - Create a project with titles that have succession pattern
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
            Titles = new List<ProjectTitle>(),
            Descriptions = null,
            LastestEdit = default,
        };

        DateTime today = DateTime.UtcNow.Date;
        DateTime yesterday = today.AddDays(-1);
        DateTime twoDaysAgo = today.AddDays(-2);

        // Old title that was ended yesterday
        ProjectTitle oldTitle = new()
        {
            Id = Guid.CreateVersion7(),
            ProjectId = project.Id,
            Text = "Old Title",
            Language = new() { Id = "eng" },
            Type = TitleType.Alternative,
            StartDate = twoDaysAgo,
            EndDate = yesterday, // This should match newTitle.StartDate
        };

        // New title created today - but we need it to start yesterday for the logic to work
        ProjectTitle newTitle = new()
        {
            Id = Guid.CreateVersion7(),
            ProjectId = project.Id,
            Text = "New Title",
            Language = new() { Id = "eng" },
            Type = TitleType.Alternative,
            StartDate = yesterday, // This should match oldTitle.EndDate
            EndDate = null,
        };

        project.Titles.Add(oldTitle);
        project.Titles.Add(newTitle);

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        // Act - Delete the new title
        await _service.DeleteTitleAsync(project.Id, newTitle.Id);

        // Assert - Old title should be restored (EndDate set to null)
        ProjectTitle? restoredTitle = await _context.ProjectTitles.FindAsync(oldTitle.Id);
        Assert.NotNull(restoredTitle);
        Assert.Null(restoredTitle.EndDate);

        // New title should be deleted
        ProjectTitle? deletedTitle = await _context.ProjectTitles.FindAsync(newTitle.Id);
        Assert.Null(deletedTitle);
    }

    [Fact]
    public async Task DeleteTitleAsync_ShouldThrow_WhenDeletingPrimaryTitleWithoutPredecessor()
    {
        // Arrange - Create project with only a primary title created today
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
            Titles = new List<ProjectTitle>(),
            Descriptions = null,
            LastestEdit = default,
        };

        DateTime today = DateTime.UtcNow.Date;

        ProjectTitle primaryTitle = new()
        {
            Id = Guid.CreateVersion7(),
            ProjectId = project.Id,
            Text = "Primary Title",
            Language = new() { Id = "eng" },
            Type = TitleType.Primary,
            StartDate = today,
            EndDate = null,
        };

        project.Titles.Add(primaryTitle);
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<CantDeleteTitleException>(async () =>
            await _service.DeleteTitleAsync(project.Id, primaryTitle.Id));
    }

    [Fact]
    public async Task UpdateTitleAsync_ShouldCreateNewTitle_WhenNoCurrentTitleOfType()
    {
        // Arrange
        Project project = await SetupDatabase();
        ProjectTitleRequestDTO dto = new()
        {
            Text = "New Acronym Title",
            Language = new() { Id = "eng" },
            Type = TitleType.Acronym,
        };

        int initialTitleCount = project.Titles.Count;

        // Act
        List<ProjectTitleResponseDTO> result = await _service.UpdateTitleAsync(project.Id, dto);

        // Assert
        Assert.Equal(initialTitleCount + 1, result.Count);
        Assert.Contains(result, t => t.Type == TitleType.Acronym && t.Text == "New Acronym Title");
    }

    [Fact]
    public async Task UpdateTitleAsync_ShouldEndPreviousAndCreateNew_WhenCurrentTitleExists()
    {
        // Arrange
        Project project = await SetupDatabase();
        ProjectTitleRequestDTO dto = new()
        {
            Text = "Updated Primary Title",
            Language = new() { Id = "nld" },
            Type = TitleType.Primary,
        };

        ProjectTitle currentPrimaryTitle = project.Titles.First(t => t.Type == TitleType.Primary && !t.EndDate.HasValue);

        // Act
        List<ProjectTitleResponseDTO> result = await _service.UpdateTitleAsync(project.Id, dto);

        // Assert
        Assert.Contains(result, t => t.Type == TitleType.Primary && t.Text == "Updated Primary Title");

        // Check that the previous title was ended
        ProjectTitle? previousTitle = await _context.ProjectTitles.FindAsync(currentPrimaryTitle.Id);
        Assert.NotNull(previousTitle);
        Assert.NotNull(previousTitle.EndDate);
        Assert.Equal(DateTime.UtcNow.Date, previousTitle.EndDate.Value.Date);
    }

    [Theory]
    [InlineData(TitleType.Primary)]
    [InlineData(TitleType.Alternative)]
    [InlineData(TitleType.Short)]
    [InlineData(TitleType.Acronym)]
    public async Task UpdateTitleAsync_ShouldHandleAllTitleTypes(TitleType titleType)
    {
        // Arrange
        Project project = await SetupDatabase();
        ProjectTitleRequestDTO dto = new()
        {
            Text = $"Test {titleType} Title",
            Language = new() { Id = "eng" },
            Type = titleType,
        };

        // Act
        List<ProjectTitleResponseDTO> result = await _service.UpdateTitleAsync(project.Id, dto);

        // Assert
        Assert.Contains(result, t => t.Type == titleType && t.Text == $"Test {titleType} Title");
    }

    [Fact]
    public async Task MapToTitleResponseDTO_ShouldMapAllProperties()
    {
        // Arrange
        Project project = await SetupDatabase();
        ProjectTitle title = project.Titles[0];

        // Act
        ProjectTitleResponseDTO result = await _service.GetTitleByIdAsync(project.Id, title.Id);

        // Assert
        Assert.Equal(title.Id, result.Id);
        Assert.Equal(title.ProjectId, result.ProjectId);
        Assert.Equal(title.Text, result.Text);
        Assert.Equal(title.Language, result.Language);
        Assert.Equal(title.Type, result.Type);
        Assert.Equal(title.StartDate, result.StartDate);
        Assert.Equal(title.EndDate, result.EndDate);
    }
}
