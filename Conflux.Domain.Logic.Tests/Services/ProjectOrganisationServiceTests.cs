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
using Moq;
using Testcontainers.PostgreSql;
using Xunit;

namespace Conflux.Domain.Logic.Tests.Services;

public class ProjectOrganisationsServiceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();
    private ConfluxContext _context = null!;
    private Mock<IProjectsService> _projectsServiceMock = null!;
    private ProjectOrganisationsService _service = null!;

    public async Task InitializeAsync()
    {
        // Use in-memory database for testing
        DbContextOptions<ConfluxContext> options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new(options);

        // Create a mock for an interface instead of the concrete class
        _projectsServiceMock = new();

        // Setup the mock to return a project response when GetProjectDTOByIdAsync is called
        _projectsServiceMock
            .Setup(m => m.GetProjectDTOByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new ProjectResponseDTO
            {
                Id = Guid.NewGuid(),
            });

        // Create the service with the mock
        _service = new(_context, _projectsServiceMock.Object);

        await _context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task GetOrganisationsByProjectIdAsync_ShouldThrow_WhenProjectDoesNotExist()
    {
        // Arrange
        Guid nonExistentProjectId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ProjectNotFoundException>(() =>
            _service.GetOrganisationsByProjectIdAsync(nonExistentProjectId));
    }

    [Fact]
    public async Task GetOrganisationsByProjectIdAsync_ShouldReturnEmptyList_WhenNoOrganisationsExist()
    {
        // Arrange
        Guid projectId = await SetupProject();

        // Act
        List<ProjectOrganisationResponseDTO> result = await _service.GetOrganisationsByProjectIdAsync(projectId);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetOrganisationsByProjectIdAsync_ShouldReturnOrganisations_WhenProjectHasOrganisations()
    {
        // Arrange
        Guid projectId = await SetupProject();
        List<Guid> organisationIds = await SetupOrganisationsForProject(projectId, 3);

        // Act
        List<ProjectOrganisationResponseDTO> result = await _service.GetOrganisationsByProjectIdAsync(projectId);

        // Assert
        Assert.Equal(3, result.Count);
        foreach (Guid orgId in organisationIds) Assert.Contains(result, o => o.Organisation.Id == orgId);
    }

    [Fact]
    public async Task GetOrganisationByIdAsync_ShouldThrow_WhenProjectOrganisationDoesNotExist()
    {
        // Arrange
        Guid projectId = await SetupProject();
        Guid nonExistentOrgId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ProjectOrganisationNotFoundException>(() =>
            _service.GetOrganisationByIdAsync(projectId, nonExistentOrgId));
    }

    [Fact]
    public async Task GetOrganisationByIdAsync_ShouldReturnOrganisation_WhenProjectOrganisationExists()
    {
        // Arrange
        Guid projectId = Guid.NewGuid();
        Guid orgId = Guid.NewGuid();

        Project project = new()
        {
            Id = projectId,
            StartDate = DateTime.UtcNow,
        };

        Organisation org = new()
        {
            Id = orgId,
            Name = "Test Organisation",
            RORId = "https://ror.org/test",
        };

        ProjectOrganisation projectOrg = new()
        {
            ProjectId = projectId,
            OrganisationId = orgId,
            Roles =
            [
                new()
                {
                    ProjectId = projectId,
                    OrganisationId = orgId,
                    Role = OrganisationRoleType.Funder,
                },
            ],
        };

        _context.Projects.Add(project);
        _context.Organisations.Add(org);
        _context.ProjectOrganisations.Add(projectOrg);
        await _context.SaveChangesAsync();

        ProjectResponseDTO projectResponseDTO = new()
        {
            Id = projectId,
            StartDate = DateTime.UtcNow,
            EndDate = null,
        };
        _projectsServiceMock.Setup(p => p.GetProjectDTOByIdAsync(projectId))
            .ReturnsAsync(projectResponseDTO);

        // Act
        ProjectOrganisationResponseDTO result = await _service.GetOrganisationByIdAsync(projectId, orgId);

        // Assert
        Assert.Equal(orgId, result.Organisation.Id);
        Assert.Equal("Test Organisation", result.Name);
        Assert.Equal("https://ror.org/test", result.RORId);
        Assert.Single(result.Roles);
        Assert.Equal(OrganisationRoleType.Funder, result.Roles[0]);
        Assert.Equal(projectId, result.Project.Id);
    }

    [Fact]
    public async Task CreateOrganisationAsync_ShouldThrow_WhenProjectDoesNotExist()
    {
        // Arrange
        Guid nonExistentProjectId = Guid.NewGuid();
        OrganisationRequestDTO dto = new()
        {
            Name = "New Organisation",
            RORId = "https://ror.org/test123",
            Roles = [],
        };

        // Act & Assert
        await Assert.ThrowsAsync<ProjectNotFoundException>(() =>
            _service.CreateOrganisationAsync(nonExistentProjectId, dto));
    }

    [Fact]
    public async Task CreateOrganisationAsync_ShouldCreateNewOrganisation_WhenOrganisationDoesNotExist()
    {
        // Arrange
        Guid projectId = Guid.NewGuid();
        Project project = new()
        {
            Id = projectId,
            StartDate = DateTime.UtcNow,
        };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        OrganisationRequestDTO dto = new()
        {
            Name = "Test Org",
            RORId = "https://ror.org/test",
            Roles =
            [
                new()
                {
                    ProjectId = projectId,
                    Role = OrganisationRoleType.Funder,
                },
            ],
        };

        ProjectResponseDTO projectResponseDTO = new()
        {
            Id = projectId,
            StartDate = DateTime.UtcNow,
            EndDate = null,
        };
        _projectsServiceMock.Setup(p => p.GetProjectDTOByIdAsync(projectId))
            .ReturnsAsync(projectResponseDTO);

        // Act
        ProjectOrganisationResponseDTO result = await _service.CreateOrganisationAsync(projectId, dto);

        // Assert
        Assert.Equal("Test Org", result.Name);
        Assert.Equal("https://ror.org/test", result.RORId);
        Assert.Single(result.Roles);
        Assert.Equal(OrganisationRoleType.Funder, result.Roles[0]);
        Assert.Equal(projectId, result.Project.Id);

        // Verify organization was created in the database
        Assert.Single(await _context.Organisations.Where(o => o.Name == "Test Org").ToListAsync());
    }

    [Fact]
    public async Task CreateOrganisationAsync_ShouldReuseExistingOrganisation_WhenOrganisationExists()
    {
        // Arrange
        Guid projectId = await SetupProject();

        // Add an organisation to the database first
        Organisation existingOrg = new()
        {
            Id = Guid.NewGuid(),
            Name = "Existing Organisation",
            RORId = "https://ror.org/existing",
        };
        _context.Organisations.Add(existingOrg);
        await _context.SaveChangesAsync();

        OrganisationRequestDTO dto = new()
        {
            Name = "Updated Name",              // Name might be updated
            RORId = "https://ror.org/existing", // But ROR ID stays the same
            Roles = [],
        };

        // Act
        ProjectOrganisationResponseDTO result = await _service.CreateOrganisationAsync(projectId, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingOrg.Id, result.Organisation.Id);
        Assert.Equal("Existing Organisation", result.Organisation.Name); // Name should not be updated
    }

    [Fact]
    public async Task CreateOrganisationAsync_ShouldThrow_WhenProjectAlreadyHasOrganisation()
    {
        // Arrange
        Guid projectId = await SetupProject();
        List<Guid> organisationIds = await SetupOrganisationsForProject(projectId, 1);
        Guid organisationId = organisationIds[0];

        // Get the existing organisation
        Organisation existingOrg = await _context.Organisations.FindAsync(organisationId);

        OrganisationRequestDTO dto = new()
        {
            Name = existingOrg.Name,
            RORId = existingOrg.RORId,
            Roles = [],
        };

        // Act & Assert
        await Assert.ThrowsAsync<ProjectAlreadyHasOrganisationException>(() =>
            _service.CreateOrganisationAsync(projectId, dto));
    }

    [Fact]
    public async Task UpdateOrganisationAsync_ShouldThrow_WhenOrganisationDoesNotExist()
    {
        // Arrange
        Guid projectId = await SetupProject();
        Guid nonExistentOrgId = Guid.NewGuid();
        OrganisationRequestDTO dto = new()
        {
            Name = "Updated Organisation",
            RORId = "https://ror.org/updated",
            Roles = [],
        };

        // Act & Assert
        await Assert.ThrowsAsync<OrganisationNotFoundException>(() =>
            _service.UpdateOrganisationAsync(projectId, nonExistentOrgId, dto));
    }


    [Fact]
    public async Task DeleteOrganisationAsync_ShouldThrow_WhenProjectOrganisationDoesNotExist()
    {
        // Arrange
        Guid projectId = await SetupProject();
        Guid nonExistentOrgId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ProjectOrganisationNotFoundException>(() =>
            _service.DeleteOrganisationAsync(projectId, nonExistentOrgId));
    }

    #region Helper Methods

    private async Task<Guid> SetupProject()
    {
        Guid projectId = Guid.NewGuid();
        Project project = new()
        {
            Id = projectId,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(6),
            Titles =
            [
                new()
                {
                    Id = Guid.NewGuid(),
                    ProjectId = projectId,
                    Text = "Test Project",
                    Type = TitleType.Primary,
                    StartDate = DateTime.UtcNow,
                },
            ],
        };

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();
        return projectId;
    }

    private async Task<List<Guid>> SetupOrganisationsForProject(Guid projectId, int count)
    {
        List<Guid> organisationIds = [];

        for (int i = 0; i < count; i++)
        {
            Guid orgId = Guid.NewGuid();
            organisationIds.Add(orgId);

            Organisation org = new()
            {
                Id = orgId,
                Name = $"Test Organisation {i}",
                RORId = $"https://ror.org/test{i}",
            };

            _context.Organisations.Add(org);

            ProjectOrganisation projectOrg = new()
            {
                ProjectId = projectId,
                OrganisationId = orgId,
                Roles = [],
            };

            _context.ProjectOrganisations.Add(projectOrg);
        }

        await _context.SaveChangesAsync();
        return organisationIds;
    }

    #endregion
}
