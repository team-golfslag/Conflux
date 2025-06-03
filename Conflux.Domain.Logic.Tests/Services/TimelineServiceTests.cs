// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain.Logic.DTOs.Responses;
using Conflux.Domain.Logic.Exceptions;
using Conflux.Domain.Logic.Services;
using Moq;
using Xunit;

namespace Conflux.Domain.Logic.Tests.Services;

public class TimelineServiceTests
{
    private readonly Mock<IProjectsService> _mockProjectsService;
    private readonly TimelineService _service;

    public TimelineServiceTests()
    {
        _mockProjectsService = new Mock<IProjectsService>();
        _service = new TimelineService(_mockProjectsService.Object);
    }

    [Fact]
    public async Task GetTimelineItemsAsync_WithValidProject_ReturnsProjectStartMilestone()
    {
        // Arrange
        Guid projectId = Guid.NewGuid();
        DateTime startDate = new(2024, 1, 15);
        Project project = CreateBasicProject(projectId, startDate);

        _mockProjectsService.Setup(s => s.GetProjectByIdAsync(projectId))
            .ReturnsAsync(project);

        // Act
        List<TimelineItemResponseDTO> result = await _service.GetTimelineItemsAsync(projectId);

        // Assert
        Assert.Contains(result, item => 
            item.IsMilestone && 
            item.Date == startDate &&
            item.ShortDescription == "Project started" &&
            item.Description == "The project started on 15 January 2024.");
    }

    [Fact]
    public async Task GetTimelineItemsAsync_WithProjectEndDate_ReturnsProjectEndMilestone()
    {
        // Arrange
        Guid projectId = Guid.NewGuid();
        DateTime startDate = new(2024, 1, 15);
        DateTime endDate = new(2024, 12, 31);
        Project project = CreateBasicProject(projectId, startDate, endDate);

        _mockProjectsService.Setup(s => s.GetProjectByIdAsync(projectId))
            .ReturnsAsync(project);

        // Act
        List<TimelineItemResponseDTO> result = await _service.GetTimelineItemsAsync(projectId);

        // Assert
        Assert.Contains(result, item => 
            item.IsMilestone && 
            item.Date == endDate &&
            item.ShortDescription == "Project ended" &&
            item.Description == "The project ended on 31 December 2024.");
    }

    [Fact]
    public async Task GetTimelineItemsAsync_WithoutProjectEndDate_DoesNotReturnProjectEndMilestone()
    {
        // Arrange
        Guid projectId = Guid.NewGuid();
        DateTime startDate = new(2024, 1, 15);
        Project project = CreateBasicProject(projectId, startDate);

        _mockProjectsService.Setup(s => s.GetProjectByIdAsync(projectId))
            .ReturnsAsync(project);

        // Act
        List<TimelineItemResponseDTO> result = await _service.GetTimelineItemsAsync(projectId);

        // Assert
        Assert.DoesNotContain(result, item => 
            item.IsMilestone && 
            item.ShortDescription == "Project ended");
    }

    [Fact]
    public async Task GetTimelineItemsAsync_WithTitles_ReturnsTitleTimelineItems()
    {
        // Arrange
        Guid projectId = Guid.NewGuid();
        DateTime startDate = new(2024, 1, 15);
        DateTime titleDate = new(2024, 2, 10);
        
        Project project = CreateBasicProject(projectId, startDate);
        project.Titles = 
        [
            new ProjectTitle
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Text = "Test Project Title",
                Type = TitleType.Primary,
                Language = new Language { Id = "eng" },
                StartDate = titleDate,
                EndDate = null
            }
        ];

        _mockProjectsService.Setup(s => s.GetProjectByIdAsync(projectId))
            .ReturnsAsync(project);

        // Act
        List<TimelineItemResponseDTO> result = await _service.GetTimelineItemsAsync(projectId);

        // Assert
        Assert.Contains(result, item => 
            !item.IsMilestone && 
            item.Date == titleDate &&
            item.ShortDescription == "Title updated" &&
            item.Description == "The project Primary-eng title was updated to 'Test Project Title' on 10 February 2024.");
    }

    [Fact]
    public async Task GetTimelineItemsAsync_WithMultipleTitles_ReturnsAllTitleTimelineItems()
    {
        // Arrange
        Guid projectId = Guid.NewGuid();
        DateTime startDate = new(2024, 1, 15);
        DateTime titleDate1 = new(2024, 2, 10);
        DateTime titleDate2 = new(2024, 3, 5);
        
        Project project = CreateBasicProject(projectId, startDate);
        project.Titles = 
        [
            new ProjectTitle
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Text = "Primary Title",
                Type = TitleType.Primary,
                Language = new Language { Id = "eng" },
                StartDate = titleDate1,
                EndDate = null
            },
            new ProjectTitle
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Text = "Short Title",
                Type = TitleType.Short,
                Language = new Language { Id = "nld" },
                StartDate = titleDate2,
                EndDate = null
            }
        ];

        _mockProjectsService.Setup(s => s.GetProjectByIdAsync(projectId))
            .ReturnsAsync(project);

        // Act
        List<TimelineItemResponseDTO> result = await _service.GetTimelineItemsAsync(projectId);

        // Assert
        var titleItems = result.Where(item => item.ShortDescription == "Title updated").ToList();
        Assert.Equal(2, titleItems.Count);
        Assert.Contains(titleItems, item => item.Description.Contains("Primary Title"));
        Assert.Contains(titleItems, item => item.Description.Contains("Short Title"));
    }

    [Fact]
    public async Task GetTimelineItemsAsync_WithOrganisations_ReturnsOrganisationTimelineItems()
    {
        // Arrange
        Guid projectId = Guid.NewGuid();
        DateTime startDate = new(2024, 1, 15);
        DateTime orgStartDate = new(2024, 2, 20);
        
        Project project = CreateBasicProject(projectId, startDate);
        Guid organisationId = Guid.NewGuid();
        project.Organisations = 
        [
            new ProjectOrganisation
            {
                ProjectId = projectId,
                OrganisationId = organisationId,
                Organisation = new Organisation
                {
                    Id = organisationId,
                    Name = "Test University",
                    RORId = "https://ror.org/test"
                },
                Roles = 
                [
                    new OrganisationRole
                    {
                        ProjectId = projectId,
                        OrganisationId = organisationId,
                        Role = OrganisationRoleType.LeadResearchOrganization,
                        StartDate = orgStartDate,
                        EndDate = null
                    }
                ]
            }
        ];

        _mockProjectsService.Setup(s => s.GetProjectByIdAsync(projectId))
            .ReturnsAsync(project);

        // Act
        List<TimelineItemResponseDTO> result = await _service.GetTimelineItemsAsync(projectId);

        // Assert
        Assert.Contains(result, item => 
            !item.IsMilestone && 
            item.Date == orgStartDate &&
            item.ShortDescription == "Organisation added" &&
            item.Description == "The organisation 'Test University' was added to the project on 20 February 2024.");
    }

    [Fact]
    public async Task GetTimelineItemsAsync_WithOrganisationEndDate_ReturnsOrganisationLeftTimelineItem()
    {
        // Arrange
        Guid projectId = Guid.NewGuid();
        DateTime startDate = new(2024, 1, 15);
        DateTime orgStartDate = new(2024, 2, 20);
        DateTime orgEndDate = new(2024, 11, 30);
        
        Project project = CreateBasicProject(projectId, startDate);
        Guid organisationId = Guid.NewGuid();
        project.Organisations = 
        [
            new ProjectOrganisation
            {
                ProjectId = projectId,
                OrganisationId = organisationId,
                Organisation = new Organisation
                {
                    Id = organisationId,
                    Name = "Test University",
                    RORId = "https://ror.org/test"
                },
                Roles = 
                [
                    new OrganisationRole
                    {
                        ProjectId = projectId,
                        OrganisationId = organisationId,
                        Role = OrganisationRoleType.LeadResearchOrganization,
                        StartDate = orgStartDate,
                        EndDate = orgEndDate
                    }
                ]
            }
        ];

        _mockProjectsService.Setup(s => s.GetProjectByIdAsync(projectId))
            .ReturnsAsync(project);

        // Act
        List<TimelineItemResponseDTO> result = await _service.GetTimelineItemsAsync(projectId);

        // Assert
        Assert.Contains(result, item => 
            !item.IsMilestone && 
            item.Date == orgEndDate &&
            item.ShortDescription == "Organisation left" &&
            item.Description == "The organisation 'Test University' left the project on 30 November 2024.");
    }

    [Fact]
    public async Task GetTimelineItemsAsync_WithOrganisationNoRoles_DoesNotReturnOrganisationItems()
    {
        // Arrange
        Guid projectId = Guid.NewGuid();
        DateTime startDate = new(2024, 1, 15);
        
        Project project = CreateBasicProject(projectId, startDate);
        Guid organisationId = Guid.NewGuid();
        project.Organisations = 
        [
            new ProjectOrganisation
            {
                ProjectId = projectId,
                OrganisationId = organisationId,
                Organisation = new Organisation
                {
                    Id = organisationId,
                    Name = "Test University",
                    RORId = "https://ror.org/test"
                },
                Roles = [] // No roles
            }
        ];

        _mockProjectsService.Setup(s => s.GetProjectByIdAsync(projectId))
            .ReturnsAsync(project);

        // Act
        List<TimelineItemResponseDTO> result = await _service.GetTimelineItemsAsync(projectId);

        // Assert
        Assert.DoesNotContain(result, item => item.ShortDescription.Contains("Organisation"));
    }

    [Fact]
    public async Task GetTimelineItemsAsync_WithContributors_ReturnsContributorTimelineItems()
    {
        // Arrange
        Guid projectId = Guid.NewGuid();
        DateTime startDate = new(2024, 1, 15);
        DateTime contributorStartDate = new(2024, 3, 1);
        
        Project project = CreateBasicProject(projectId, startDate);
        Guid contributorPersonId = Guid.NewGuid();
        project.Contributors = 
        [
            new Contributor
            {
                PersonId = contributorPersonId,
                ProjectId = projectId,
                Person = new Person
                {
                    Id = contributorPersonId,
                    Name = "John Doe",
                    Email = "john.doe@example.com"
                },
                Positions = 
                [
                    new ContributorPosition
                    {
                        PersonId = contributorPersonId,
                        ProjectId = projectId,
                        Position = ContributorPositionType.PrincipalInvestigator,
                        StartDate = contributorStartDate,
                        EndDate = null
                    }
                ]
            }
        ];

        _mockProjectsService.Setup(s => s.GetProjectByIdAsync(projectId))
            .ReturnsAsync(project);

        // Act
        List<TimelineItemResponseDTO> result = await _service.GetTimelineItemsAsync(projectId);

        // Assert
        Assert.Contains(result, item => 
            !item.IsMilestone && 
            item.Date == contributorStartDate &&
            item.ShortDescription == "Contributor added" &&
            item.Description == "The contributor 'John Doe' was added to the project on 01 March 2024.");
    }

    [Fact]
    public async Task GetTimelineItemsAsync_WithContributorEndDate_ReturnsContributorLeftTimelineItem()
    {
        // Arrange
        Guid projectId = Guid.NewGuid();
        DateTime startDate = new(2024, 1, 15);
        DateTime contributorStartDate = new(2024, 3, 1);
        DateTime contributorEndDate = new(2024, 10, 15);
        
        Project project = CreateBasicProject(projectId, startDate);
        Guid contributorPersonId = Guid.NewGuid();
        project.Contributors = 
        [
            new Contributor
            {
                PersonId = contributorPersonId,
                ProjectId = projectId,
                Person = new Person
                {
                    Id = contributorPersonId,
                    Name = "John Doe",
                    Email = "john.doe@example.com"
                },
                Positions = 
                [
                    new ContributorPosition
                    {
                        PersonId = contributorPersonId,
                        ProjectId = projectId,
                        Position = ContributorPositionType.PrincipalInvestigator,
                        StartDate = contributorStartDate,
                        EndDate = contributorEndDate
                    }
                ]
            }
        ];

        _mockProjectsService.Setup(s => s.GetProjectByIdAsync(projectId))
            .ReturnsAsync(project);

        // Act
        List<TimelineItemResponseDTO> result = await _service.GetTimelineItemsAsync(projectId);

        // Assert
        Assert.Contains(result, item => 
            !item.IsMilestone && 
            item.Date == contributorEndDate &&
            item.ShortDescription == "Contributor left" &&
            item.Description == "The contributor 'John Doe' left the project on 15 October 2024.");
    }

    [Fact]
    public async Task GetTimelineItemsAsync_WithContributorNoPositions_DoesNotReturnContributorItems()
    {
        // Arrange
        Guid projectId = Guid.NewGuid();
        DateTime startDate = new(2024, 1, 15);
        
        Project project = CreateBasicProject(projectId, startDate);
        Guid personId = Guid.NewGuid();
        project.Contributors = 
        [
            new Contributor
            {
                PersonId = personId,
                ProjectId = projectId,
                Person = new Person
                {
                    Id = personId,
                    Name = "John Doe",
                    Email = "john.doe@example.com"
                },
                Positions = [] // No positions
            }
        ];

        _mockProjectsService.Setup(s => s.GetProjectByIdAsync(projectId))
            .ReturnsAsync(project);

        // Act
        List<TimelineItemResponseDTO> result = await _service.GetTimelineItemsAsync(projectId);

        // Assert
        Assert.DoesNotContain(result, item => item.ShortDescription.Contains("Contributor"));
    }

    [Fact]
    public async Task GetTimelineItemsAsync_WithMultipleRoles_UsesEarliestStartDateForOrganisation()
    {
        // Arrange
        Guid projectId = Guid.NewGuid();
        DateTime startDate = new(2024, 1, 15);
        DateTime role1StartDate = new(2024, 2, 20);
        DateTime role2StartDate = new(2024, 1, 25); // Earlier date
        
        Project project = CreateBasicProject(projectId, startDate);
        Guid organisationId = Guid.NewGuid();
        project.Organisations = 
        [
            new ProjectOrganisation
            {
                ProjectId = projectId,
                OrganisationId = organisationId,
                Organisation = new Organisation
                {
                    Id = organisationId,
                    Name = "Test University",
                    RORId = "https://ror.org/test"
                },
                Roles = 
                [
                    new OrganisationRole
                    {
                        ProjectId = projectId,
                        OrganisationId = organisationId,
                        Role = OrganisationRoleType.LeadResearchOrganization,
                        StartDate = role1StartDate,
                        EndDate = null
                    },
                    new OrganisationRole
                    {
                        ProjectId = projectId,
                        OrganisationId = organisationId,
                        Role = OrganisationRoleType.Contractor,
                        StartDate = role2StartDate, // Earlier date
                        EndDate = null
                    }
                ]
            }
        ];

        _mockProjectsService.Setup(s => s.GetProjectByIdAsync(projectId))
            .ReturnsAsync(project);

        // Act
        List<TimelineItemResponseDTO> result = await _service.GetTimelineItemsAsync(projectId);

        // Assert
        Assert.Contains(result, item => 
            !item.IsMilestone && 
            item.Date == role2StartDate && // Should use the earlier date
            item.ShortDescription == "Organisation added");
    }

    [Fact]
    public async Task GetTimelineItemsAsync_WithMultiplePositions_UsesEarliestStartDateForContributor()
    {
        // Arrange
        Guid projectId = Guid.NewGuid();
        DateTime startDate = new(2024, 1, 15);
        DateTime position1StartDate = new(2024, 3, 1);
        DateTime position2StartDate = new(2024, 2, 15); // Earlier date
        
        Project project = CreateBasicProject(projectId, startDate);
        Guid personId = Guid.NewGuid();
        project.Contributors = 
        [
            new Contributor
            {
                PersonId = personId,
                ProjectId = projectId,
                Person = new Person
                {
                    Id = personId,
                    Name = "John Doe",
                    Email = "john.doe@example.com"
                },
                Positions = 
                [
                    new ContributorPosition
                    {
                        PersonId = personId,
                        ProjectId = projectId,
                        Position = ContributorPositionType.PrincipalInvestigator,
                        StartDate = position1StartDate,
                        EndDate = null
                    },
                    new ContributorPosition
                    {
                        PersonId = personId,
                        ProjectId = projectId,
                        Position = ContributorPositionType.Consultant,
                        StartDate = position2StartDate, // Earlier date
                        EndDate = null
                    }
                ]
            }
        ];

        _mockProjectsService.Setup(s => s.GetProjectByIdAsync(projectId))
            .ReturnsAsync(project);

        // Act
        List<TimelineItemResponseDTO> result = await _service.GetTimelineItemsAsync(projectId);

        // Assert
        Assert.Contains(result, item => 
            !item.IsMilestone && 
            item.Date == position2StartDate && // Should use the earlier date
            item.ShortDescription == "Contributor added");
    }

    [Fact]
    public async Task GetTimelineItemsAsync_WithMixedPositionEndDates_UsesLatestEndDateForContributor()
    {
        // Arrange
        Guid projectId = Guid.NewGuid();
        DateTime startDate = new(2024, 1, 15);
        DateTime contributorStartDate = new(2024, 3, 1);
        DateTime position1EndDate = new(2024, 8, 15);
        DateTime position2EndDate = new(2024, 10, 31); // Later date
        
        Project project = CreateBasicProject(projectId, startDate);
        Guid personId = Guid.NewGuid();
        project.Contributors = 
        [
            new Contributor
            {
                PersonId = personId,
                ProjectId = projectId,
                Person = new Person
                {
                    Id = personId,
                    Name = "John Doe",
                    Email = "john.doe@example.com"
                },
                Positions = 
                [
                    new ContributorPosition
                    {
                        PersonId = personId,
                        ProjectId = projectId,
                        Position = ContributorPositionType.PrincipalInvestigator,
                        StartDate = contributorStartDate,
                        EndDate = position1EndDate
                    },
                    new ContributorPosition
                    {
                        PersonId = personId,
                        ProjectId = projectId,
                        Position = ContributorPositionType.Consultant,
                        StartDate = contributorStartDate,
                        EndDate = position2EndDate // Later date
                    }
                ]
            }
        ];

        _mockProjectsService.Setup(s => s.GetProjectByIdAsync(projectId))
            .ReturnsAsync(project);

        // Act
        List<TimelineItemResponseDTO> result = await _service.GetTimelineItemsAsync(projectId);

        // Assert
        Assert.Contains(result, item => 
            !item.IsMilestone && 
            item.Date == position2EndDate && // Should use the later end date
            item.ShortDescription == "Contributor left");
    }

    [Fact]
    public async Task GetTimelineItemsAsync_WithSomePositionsWithoutEndDate_DoesNotReturnContributorLeftItem()
    {
        // Arrange
        Guid projectId = Guid.NewGuid();
        DateTime startDate = new(2024, 1, 15);
        DateTime contributorStartDate = new(2024, 3, 1);
        DateTime position1EndDate = new(2024, 8, 15);
        
        Project project = CreateBasicProject(projectId, startDate);
        Guid personId = Guid.NewGuid();
        project.Contributors = 
        [
            new Contributor
            {
                PersonId = personId,
                ProjectId = projectId,
                Person = new Person
                {
                    Id = personId,
                    Name = "John Doe",
                    Email = "john.doe@example.com"
                },
                Positions = 
                [
                    new ContributorPosition
                    {
                        PersonId = personId,
                        ProjectId = projectId,
                        Position = ContributorPositionType.PrincipalInvestigator,
                        StartDate = contributorStartDate,
                        EndDate = position1EndDate
                    },
                    new ContributorPosition
                    {
                        PersonId = personId,
                        ProjectId = projectId,
                        Position = ContributorPositionType.Consultant,
                        StartDate = contributorStartDate,
                        EndDate = null // Still active
                    }
                ]
            }
        ];

        _mockProjectsService.Setup(s => s.GetProjectByIdAsync(projectId))
            .ReturnsAsync(project);

        // Act
        List<TimelineItemResponseDTO> result = await _service.GetTimelineItemsAsync(projectId);

        // Assert
        Assert.DoesNotContain(result, item => 
            item.ShortDescription == "Contributor left");
    }

    [Fact]
    public async Task GetTimelineItemsAsync_WithNullOrganisation_ReturnsEmptyDescriptionForOrganisationItems()
    {
        // Arrange
        Guid projectId = Guid.NewGuid();
        DateTime startDate = new(2024, 1, 15);
        DateTime orgStartDate = new(2024, 2, 20);
        
        Project project = CreateBasicProject(projectId, startDate);
        Guid organisationId = Guid.NewGuid();
        project.Organisations = 
        [
            new ProjectOrganisation
            {
                ProjectId = projectId,
                OrganisationId = organisationId,
                Organisation = null, // Null organisation
                Roles = 
                [
                    new OrganisationRole
                    {
                        ProjectId = projectId,
                        OrganisationId = organisationId,
                        Role = OrganisationRoleType.LeadResearchOrganization,
                        StartDate = orgStartDate,
                        EndDate = null
                    }
                ]
            }
        ];

        _mockProjectsService.Setup(s => s.GetProjectByIdAsync(projectId))
            .ReturnsAsync(project);

        // Act
        List<TimelineItemResponseDTO> result = await _service.GetTimelineItemsAsync(projectId);

        // Assert
        Assert.Contains(result, item => 
            !item.IsMilestone && 
            item.Date == orgStartDate &&
            item.ShortDescription == "Organisation added" &&
            item.Description == "");
    }

    [Fact]
    public async Task GetTimelineItemsAsync_WithNullPerson_ReturnsEmptyDescriptionForContributorItems()
    {
        // Arrange
        Guid projectId = Guid.NewGuid();
        DateTime startDate = new(2024, 1, 15);
        DateTime contributorStartDate = new(2024, 3, 1);
        
        Project project = CreateBasicProject(projectId, startDate);
        Guid personId = Guid.NewGuid();
        project.Contributors = 
        [
            new Contributor
            {
                PersonId = personId,
                ProjectId = projectId,
                Person = null, // Null person
                Positions = 
                [
                    new ContributorPosition
                    {
                        PersonId = personId,
                        ProjectId = projectId,
                        Position = ContributorPositionType.PrincipalInvestigator,
                        StartDate = contributorStartDate,
                        EndDate = null
                    }
                ]
            }
        ];

        _mockProjectsService.Setup(s => s.GetProjectByIdAsync(projectId))
            .ReturnsAsync(project);

        // Act
        List<TimelineItemResponseDTO> result = await _service.GetTimelineItemsAsync(projectId);

        // Assert
        Assert.Contains(result, item => 
            !item.IsMilestone && 
            item.Date == contributorStartDate &&
            item.ShortDescription == "Contributor added" &&
            item.Description == "");
    }

    [Fact]
    public async Task GetTimelineItemsAsync_ReturnsItemsOrderedByDateDescending()
    {
        // Arrange
        Guid projectId = Guid.NewGuid();
        DateTime startDate = new(2024, 1, 15);
        DateTime endDate = new(2024, 12, 31);
        DateTime titleDate = new(2024, 6, 15);
        DateTime orgDate = new(2024, 3, 10);
        DateTime contributorDate = new(2024, 9, 5);
        
        Project project = CreateBasicProject(projectId, startDate, endDate);
        project.Titles = 
        [
            new ProjectTitle
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Text = "Test Title",
                Type = TitleType.Primary,
                Language = new Language { Id = "eng" },
                StartDate = titleDate,
                EndDate = null
            }
        ];
        Guid organisationId = Guid.NewGuid();
        project.Organisations = 
        [
            new ProjectOrganisation
            {
                ProjectId = projectId,
                OrganisationId = organisationId,
                Organisation = new Organisation { Id = organisationId, Name = "Test Org", RORId = "test" },
                Roles = 
                [
                    new OrganisationRole
                    {
                        ProjectId = projectId,
                        OrganisationId = organisationId,
                        Role = OrganisationRoleType.LeadResearchOrganization,
                        StartDate = orgDate,
                        EndDate = null
                    }
                ]
            }
        ];
        Guid contributorPersonId = Guid.NewGuid();
        project.Contributors = 
        [
            new Contributor
            {
                PersonId = contributorPersonId,
                ProjectId = projectId,
                Person = new Person { Id = contributorPersonId, Name = "Test Person", Email = "test@example.com" },
                Positions = 
                [
                    new ContributorPosition
                    {
                        PersonId = contributorPersonId,
                        ProjectId = projectId,
                        Position = ContributorPositionType.PrincipalInvestigator,
                        StartDate = contributorDate,
                        EndDate = null
                    }
                ]
            }
        ];

        _mockProjectsService.Setup(s => s.GetProjectByIdAsync(projectId))
            .ReturnsAsync(project);

        // Act
        List<TimelineItemResponseDTO> result = await _service.GetTimelineItemsAsync(projectId);

        // Assert
        Assert.True(result.Count >= 5); // At least 5 items: start, end, title, org, contributor
        
        // Verify descending order
        for (int i = 1; i < result.Count; i++)
        {
            Assert.True(result[i - 1].Date >= result[i].Date, 
                $"Items not in descending order: {result[i - 1].Date} should be >= {result[i].Date}");
        }
        
        // The most recent item should be the project end
        Assert.Equal(endDate, result[0].Date);
        Assert.Equal("Project ended", result[0].ShortDescription);
    }

    [Fact]
    public async Task GetTimelineItemsAsync_WithEmptyCollections_ReturnsOnlyProjectMilestones()
    {
        // Arrange
        Guid projectId = Guid.NewGuid();
        DateTime startDate = new(2024, 1, 15);
        DateTime endDate = new(2024, 12, 31);
        
        Project project = CreateBasicProject(projectId, startDate, endDate);
        // All collections are empty by default

        _mockProjectsService.Setup(s => s.GetProjectByIdAsync(projectId))
            .ReturnsAsync(project);

        // Act
        List<TimelineItemResponseDTO> result = await _service.GetTimelineItemsAsync(projectId);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, item => Assert.True(item.IsMilestone));
        Assert.Contains(result, item => item.ShortDescription == "Project started");
        Assert.Contains(result, item => item.ShortDescription == "Project ended");
    }

    [Fact]
    public async Task GetTimelineItemsAsync_WhenProjectsServiceThrows_PropagatesException()
    {
        // Arrange
        Guid projectId = Guid.NewGuid();
        _mockProjectsService.Setup(s => s.GetProjectByIdAsync(projectId))
            .ThrowsAsync(new ProjectNotFoundException(projectId));

        // Act & Assert
        await Assert.ThrowsAsync<ProjectNotFoundException>(() => 
            _service.GetTimelineItemsAsync(projectId));
    }

    #region Helper Methods

    private static Project CreateBasicProject(Guid projectId, DateTime startDate, DateTime? endDate = null)
    {
        return new Project
        {
            Id = projectId,
            SCIMId = "test-scim-id",
            StartDate = startDate,
            EndDate = endDate,
            Titles = [],
            Organisations = [],
            Contributors = [],
            Users = [],
            Products = [],
            Descriptions = [],
            LastestEdit = DateTime.UtcNow
        };
    }

    #endregion
}
