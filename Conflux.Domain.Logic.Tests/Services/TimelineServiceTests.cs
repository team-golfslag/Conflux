// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
//
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain.Logic.DTOs.Responses;
using Conflux.Domain.Logic.Exceptions;
using Conflux.Domain.Logic.Services;
using Moq;
using Xunit;
using Xunit.Sdk;

namespace Conflux.Domain.Logic.Tests.Services;

public class TimelineServiceTests
{
    private readonly Mock<IProjectsService> _mockProjectsService;
    private readonly TimelineService _service;

    public TimelineServiceTests()
    {
        _mockProjectsService = new();
        _service = new(_mockProjectsService.Object);
    }

    private void SetupMockProjectService(Project project)
    {
        _mockProjectsService.Setup(s => s.GetProjectByIdAsync(project.Id))
            .ReturnsAsync(project);
    }

    private static Project CreateBasicProject(Guid projectId, DateTime startDate, DateTime? endDate = null) =>
        new()
        {
            Id = projectId,
            StartDate = startDate,
            EndDate = endDate,
            Titles = [],
            Organisations = [],
            Contributors = [],
            Users = [],
            Products = [],
            Descriptions = []
        };

    [Fact]
    public async Task GetTimelineItemsAsync_WithValidProject_ReturnsProjectStartMilestone()
    {
        // Arrange
        DateTime startDate = new(2024, 1, 15);
        Project project = CreateBasicProject(Guid.CreateVersion7(), startDate);
        SetupMockProjectService(project);

        // Act
        List<TimelineItemResponseDTO> result = await _service.GetTimelineItemsAsync(project.Id);

        // Assert
        TimelineItemResponseDTO item = Assert.Single(result, i => i.ShortDescription == "Project started");
        Assert.True(item.IsMilestone);
        Assert.Equal(startDate, item.Date);
        Assert.Equal("The project started on 15 January 2024.", item.Description);
    }

    [Fact]
    public async Task GetTimelineItemsAsync_WithProjectEndDate_ReturnsProjectEndMilestone()
    {
        // Arrange
        DateTime endDate = new(2024, 12, 31);
        Project project = CreateBasicProject(Guid.CreateVersion7(), DateTime.Now, endDate);
        SetupMockProjectService(project);

        // Act
        List<TimelineItemResponseDTO> result = await _service.GetTimelineItemsAsync(project.Id);

        // Assert
        TimelineItemResponseDTO item = Assert.Single(result, i => i.ShortDescription == "Project ended");
        Assert.True(item.IsMilestone);
        Assert.Equal(endDate, item.Date);
        Assert.Equal("The project ended on 31 December 2024.", item.Description);
    }

    [Fact]
    public async Task GetTimelineItemsAsync_WithTitles_ReturnsTitleTimelineItems()
    {
        // Arrange
        DateTime titleDate = new(2024, 2, 10);
        Project project = CreateBasicProject(Guid.CreateVersion7(), DateTime.Now);
        project.Titles = [
            new ProjectTitle
            {
                Text = "Test Project Title",
                Type = TitleType.Primary,
                Language = new()
                    { Id = "eng" },
                StartDate = titleDate
            }
        ];
        SetupMockProjectService(project);

        // Act
        List<TimelineItemResponseDTO> result = await _service.GetTimelineItemsAsync(project.Id);

        // Assert
        TimelineItemResponseDTO item = Assert.Single(result, i => i.ShortDescription == "Title updated");
        Assert.False(item.IsMilestone);
        Assert.Equal(titleDate, item.Date);
        Assert.Equal("The project Primary-eng title was updated to 'Test Project Title' on 10 February 2024.", item.Description);
    }

    [Fact]
    public async Task GetTimelineItemsAsync_WithOrganisations_ReturnsOrganisationTimelineItems()
    {
        // Arrange
        DateTime orgStartDate = new(2024, 2, 20);
        Project project = CreateBasicProject(Guid.CreateVersion7(), DateTime.Now);
        project.Organisations = [
            new ProjectOrganisation
            {
                Organisation = new()
                    { Name = "Test University" },
                Roles = [new OrganisationRole { StartDate = orgStartDate }]
            }
        ];
        SetupMockProjectService(project);

        // Act
        List<TimelineItemResponseDTO> result = await _service.GetTimelineItemsAsync(project.Id);

        // Assert
        TimelineItemResponseDTO item = Assert.Single(result, i => i.ShortDescription == "Organisation added");
        Assert.False(item.IsMilestone);
        Assert.Equal(orgStartDate, item.Date);
        Assert.Equal("The organisation 'Test University' was added to the project on 20 February 2024.", item.Description);
    }

    [Fact]
    public async Task GetTimelineItemsAsync_WithOrganisationEndDate_ReturnsOrganisationLeftTimelineItem()
    {
        // Arrange
        DateTime orgEndDate = new(2024, 11, 30);
        Project project = CreateBasicProject(Guid.CreateVersion7(), DateTime.Now);
        project.Organisations = [
            new ProjectOrganisation
            {
                Organisation = new()
                    { Name = "Test University" },
                Roles = [new OrganisationRole { StartDate = DateTime.Now, EndDate = orgEndDate }]
            }
        ];
        SetupMockProjectService(project);

        // Act
        List<TimelineItemResponseDTO> result = await _service.GetTimelineItemsAsync(project.Id);

        // Assert
        TimelineItemResponseDTO item = Assert.Single(result, i => i.ShortDescription == "Organisation left");
        Assert.False(item.IsMilestone);
        Assert.Equal(orgEndDate, item.Date);
        Assert.Equal("The organisation 'Test University' left the project on 30 November 2024.", item.Description);
    }

    [Fact]
    public async Task GetTimelineItemsAsync_WithContributors_ReturnsContributorTimelineItems()
    {
        // Arrange
        DateTime contributorStartDate = new(2024, 3, 1);
        Project project = CreateBasicProject(Guid.CreateVersion7(), DateTime.Now);
        project.Contributors = [
            new Contributor
            {
                Person = new()
                    { Name = "John Doe" },
                Positions = [new ContributorPosition { StartDate = contributorStartDate, Position = ContributorPositionType.Other }]
            }
        ];
        SetupMockProjectService(project);

        // Act
        List<TimelineItemResponseDTO> result = await _service.GetTimelineItemsAsync(project.Id);

        // Assert
        TimelineItemResponseDTO item = Assert.Single(result, i => i.ShortDescription == "Contributor added");
        Assert.False(item.IsMilestone);
        Assert.Equal(contributorStartDate, item.Date);
        Assert.Equal("The contributor 'John Doe' was added to the project on 01 March 2024.", item.Description);
    }

    [Fact]
    public async Task GetTimelineItemsAsync_WithContributorEndDate_ReturnsContributorLeftTimelineItem()
    {
        // Arrange
        DateTime contributorEndDate = new(2024, 10, 15);
        Project project = CreateBasicProject(Guid.CreateVersion7(), DateTime.Now);
        project.Contributors = [
            new Contributor
            {
                Person = new()
                    { Name = "John Doe" },
                Positions = [new ContributorPosition { StartDate = DateTime.Now, EndDate = contributorEndDate, Position = ContributorPositionType.Other }]
            }
        ];
        SetupMockProjectService(project);

        // Act
        List<TimelineItemResponseDTO> result = await _service.GetTimelineItemsAsync(project.Id);

        // Assert
        TimelineItemResponseDTO item = Assert.Single(result, i => i.ShortDescription == "Contributor left");
        Assert.False(item.IsMilestone);
        Assert.Equal(contributorEndDate, item.Date);
        Assert.Equal("The contributor 'John Doe' left the project on 15 October 2024.", item.Description);
    }

    [Fact]
    public async Task GetTimelineItemsAsync_WithMultiplePositions_UsesEarliestStartDateForContributor()
    {
        // Arrange
        DateTime earliestDate = new(2024, 2, 15);
        Project project = CreateBasicProject(Guid.CreateVersion7(), DateTime.Now);
        project.Contributors = [
            new Contributor
            {
                Person = new()
                    { Name = "John Doe" },
                Positions = [
                    new ContributorPosition { StartDate = new(2024, 3, 1), Position = ContributorPositionType.Other },
                    new ContributorPosition { StartDate = earliestDate, Position = ContributorPositionType.Other }
                ]
            }
        ];
        SetupMockProjectService(project);

        // Act
        List<TimelineItemResponseDTO> result = await _service.GetTimelineItemsAsync(project.Id);

        // Assert
        TimelineItemResponseDTO item = Assert.Single(result, i => i.ShortDescription == "Contributor added");
        Assert.Equal(earliestDate, item.Date);
    }

    [Fact]
    public async Task GetTimelineItemsAsync_WithMixedPositionEndDates_UsesLatestEndDateForContributor()
    {
        // Arrange
        DateTime latestDate = new(2024, 10, 31);
        Project project = CreateBasicProject(Guid.CreateVersion7(), DateTime.Now);
        project.Contributors = [
            new Contributor
            {
                Person = new()
                    { Name = "John Doe" },
                Positions = [
                    new ContributorPosition { StartDate = DateTime.Now, EndDate = new DateTime(2024, 8, 15), Position = ContributorPositionType.Other },
                    new ContributorPosition { StartDate = DateTime.Now, EndDate = latestDate, Position = ContributorPositionType.Other }
                ]
            }
        ];
        SetupMockProjectService(project);

        // Act
        List<TimelineItemResponseDTO> result = await _service.GetTimelineItemsAsync(project.Id);

        // Assert
        TimelineItemResponseDTO item = Assert.Single(result, i => i.ShortDescription == "Contributor left");
        Assert.Equal(latestDate, item.Date);
    }

    [Fact]
    public async Task GetTimelineItemsAsync_WithSomePositionsWithoutEndDate_DoesNotReturnContributorLeftItem()
    {
        // Arrange
        Project project = CreateBasicProject(Guid.CreateVersion7(), DateTime.Now);
        project.Contributors = [
            new Contributor
            {
                Person = new()
                    { Name = "John Doe" },
                Positions = [
                    new ContributorPosition { StartDate = DateTime.Now, EndDate = new DateTime(2024, 8, 15), Position = ContributorPositionType.Other },
                    new ContributorPosition { StartDate = DateTime.Now, EndDate = null, Position = ContributorPositionType.Other } // Still active
                ]
            }
        ];
        SetupMockProjectService(project);

        // Act
        List<TimelineItemResponseDTO> result = await _service.GetTimelineItemsAsync(project.Id);

        // Assert
        Assert.DoesNotContain(result, item => item.ShortDescription == "Contributor left");
    }

    [Fact]
    public async Task GetTimelineItemsAsync_ReturnsItemsOrderedByDateDescending()
    {
        // Arrange
        Project project = CreateBasicProject(Guid.CreateVersion7(), new(2024, 1, 15), new DateTime(2024, 12, 31));
        project.Titles = [new ProjectTitle
            {
                StartDate = new(2024,
                    6,
                    15),
                Text = "T",
                Language = new()
                {
                    Id = "l"
                },
                Type = TitleType.Primary
            }
        ];
        project.Organisations = [new ProjectOrganisation { Organisation = new()
            { Name = "O" }, Roles = [new OrganisationRole { StartDate = new(2024, 3, 10) }] }];
        project.Contributors = [new Contributor { Person = new()
            { Name = "P" }, Positions = [new ContributorPosition { StartDate = new(2024, 9, 5), Position = ContributorPositionType.Other }] }];
        SetupMockProjectService(project);

        // Act
        List<TimelineItemResponseDTO> result = await _service.GetTimelineItemsAsync(project.Id);

        // Assert
        Assert.Equal(result.OrderByDescending(i => i.Date).ToList(), result);
        Assert.Equal("Project ended", result.First().ShortDescription);
        Assert.Equal("Project started", result.Last().ShortDescription);
    }

    [Fact]
    public async Task GetTimelineItemsAsync_WhenProjectsServiceThrows_PropagatesException()
    {
        // Arrange
        Guid projectId = Guid.CreateVersion7();
        _mockProjectsService.Setup(s => s.GetProjectByIdAsync(projectId))
            .ThrowsAsync(new ProjectNotFoundException(projectId));

        // Act & Assert
        await Assert.ThrowsAsync<ProjectNotFoundException>(() => _service.GetTimelineItemsAsync(projectId));
    }
}