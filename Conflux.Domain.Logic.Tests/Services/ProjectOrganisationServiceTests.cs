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
using ROR.Net.Services;
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
        _service = new(_context, _projectsServiceMock.Object, new Mock<OrganizationService>().Object);

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
        Assert.Equal("Test Organisation", result.Organisation.Name);
        Assert.Equal("https://ror.org/test", result.Organisation.RORId);
        Assert.Single(result.Organisation.Roles);
        Assert.Equal(OrganisationRoleType.Funder, result.Organisation.Roles[0].Role);
        Assert.Equal(projectId, result.ProjectId);
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
            Role = OrganisationRoleType.Funder,
        };

        // Act & Assert
        await Assert.ThrowsAsync<ProjectNotFoundException>(() =>
            _service.CreateOrganisationAsync(nonExistentProjectId, dto));
    }

    [Fact]
    public async Task CreateOrganisationAsync_ShouldThrow_WhenRoleIsNull()
    {
        // Arrange
        Guid projectId = await SetupProject();
        OrganisationRequestDTO dto = new()
        {
            Name = "New Organisation",
            RORId = "https://ror.org/test123",
            Role = null,
        };

        // Act & Assert
        await Assert.ThrowsAsync<ProjectOrganisationException>(() =>
            _service.CreateOrganisationAsync(projectId, dto));
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
            Role =
               
                    OrganisationRoleType.Funder,
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
        Assert.Equal("Test Org", result.Organisation.Name);
        Assert.Equal("https://ror.org/test", result.Organisation.RORId);
        Assert.Single(result.Organisation.Roles);
        Assert.Equal(OrganisationRoleType.Funder, result.Organisation.Roles[0].Role);
        Assert.Equal(projectId, result.ProjectId);

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
            Role = OrganisationRoleType.Contractor,
        };

        // Act
        ProjectOrganisationResponseDTO result = await _service.CreateOrganisationAsync(projectId, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingOrg.Id, result.Organisation.Id);
        Assert.Equal("Existing Organisation", result.Organisation.Name); // Name should not be updated when reusing existing organisation
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
            Role = OrganisationRoleType.Funder,
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
            Role = OrganisationRoleType.Funder,
        };

        // Act & Assert
        await Assert.ThrowsAsync<OrganisationNotFoundException>(() =>
            _service.UpdateOrganisationAsync(projectId, nonExistentOrgId, dto));
    }

    [Fact]
    public async Task UpdateOrganisationAsync_ShouldUpdateOrganisationAndCreateNewRole_WhenRoleIsNew()
    {
        // Arrange
        Guid projectId = await SetupProject();
        Guid organisationId = await SetupSingleOrganisationForProject(projectId, OrganisationRoleType.Funder);

        OrganisationRequestDTO dto = new()
        {
            Name = "Updated Organisation Name",
            RORId = "https://ror.org/updated",
            Role = OrganisationRoleType.Contractor,
        };

        // Act
        ProjectOrganisationResponseDTO result = await _service.UpdateOrganisationAsync(projectId, organisationId, dto);

        // Assert
        Assert.Equal("Updated Organisation Name", result.Organisation.Name);
        Assert.Equal("https://ror.org/updated", result.Organisation.RORId);
        
        // Should have 2 roles - the old one with an end date, and the new one without
        Assert.Equal(2, result.Organisation.Roles.Count);
        
        OrganisationRoleResponseDTO endedRole = result.Organisation.Roles.First(r => r.EndDate.HasValue);
        OrganisationRoleResponseDTO activeRole = result.Organisation.Roles.First(r => !r.EndDate.HasValue);
        
        Assert.Equal(OrganisationRoleType.Funder, endedRole.Role);
        Assert.Equal(OrganisationRoleType.Contractor, activeRole.Role);
        Assert.True(endedRole.EndDate.HasValue);
        Assert.False(activeRole.EndDate.HasValue);
    }

    [Fact]
    public async Task UpdateOrganisationAsync_ShouldCreateNewRole_WhenRoleWasPreviouslyEnded()
    {
        // Arrange
        Guid projectId = await SetupProject();
        Guid organisationId = await SetupOrganisationWithRoleHistory(projectId);

        OrganisationRequestDTO dto = new()
        {
            Name = "Updated Organisation Name",
            RORId = "https://ror.org/updated",
            Role = OrganisationRoleType.Funder, // Create new Funder role (even though one existed before)
        };

        // Act
        ProjectOrganisationResponseDTO result = await _service.UpdateOrganisationAsync(projectId, organisationId, dto);

        // Assert
        Assert.Equal("Updated Organisation Name", result.Organisation.Name);
        
        // Should have 3 roles total: old ended Funder, ended Contractor, and new active Funder
        Assert.Equal(3, result.Organisation.Roles.Count);
        
        List<OrganisationRoleResponseDTO> funderRoles = result.Organisation.Roles
            .Where(r => r.Role == OrganisationRoleType.Funder).ToList();
        OrganisationRoleResponseDTO contractorRole = result.Organisation.Roles
            .First(r => r.Role == OrganisationRoleType.Contractor);
        
        // Should have 2 Funder roles: old one (ended) and new one (active)
        Assert.Equal(2, funderRoles.Count);
        Assert.Single(funderRoles.Where(r => r.EndDate.HasValue)); // Old ended Funder
        Assert.Single(funderRoles.Where(r => !r.EndDate.HasValue)); // New active Funder
        
        // Contractor role should be ended
        Assert.True(contractorRole.EndDate.HasValue);
    }

    [Fact]
    public async Task UpdateOrganisationAsync_ShouldEndCurrentRole_WhenRoleIsSetToNull()
    {
        // Arrange
        Guid projectId = await SetupProject();
        Guid organisationId = await SetupSingleOrganisationForProject(projectId, OrganisationRoleType.Funder);

        OrganisationRequestDTO dto = new()
        {
            Name = "Updated Organisation Name",
            RORId = "https://ror.org/updated",
            Role = null, // Remove current role
        };

        // Act
        ProjectOrganisationResponseDTO result = await _service.UpdateOrganisationAsync(projectId, organisationId, dto);

        // Assert
        Assert.Equal("Updated Organisation Name", result.Organisation.Name);
        
        // Should have 1 role with an end date
        Assert.Single(result.Organisation.Roles);
        Assert.True(result.Organisation.Roles[0].EndDate.HasValue);
        Assert.Equal(OrganisationRoleType.Funder, result.Organisation.Roles[0].Role);
    }

    [Fact]
    public async Task UpdateOrganisationAsync_ShouldNotChangeRoles_WhenRoleIsUnchanged()
    {
        // Arrange
        Guid projectId = await SetupProject();
        Guid organisationId = await SetupSingleOrganisationForProject(projectId, OrganisationRoleType.Funder);

        OrganisationRequestDTO dto = new()
        {
            Name = "Updated Organisation Name",
            RORId = "https://ror.org/updated",
            Role = OrganisationRoleType.Funder, // Same role as current
        };

        // Act
        ProjectOrganisationResponseDTO result = await _service.UpdateOrganisationAsync(projectId, organisationId, dto);

        // Assert
        Assert.Equal("Updated Organisation Name", result.Organisation.Name);
        
        // Should have 1 role without an end date
        Assert.Single(result.Organisation.Roles);
        Assert.False(result.Organisation.Roles[0].EndDate.HasValue);
        Assert.Equal(OrganisationRoleType.Funder, result.Organisation.Roles[0].Role);
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

    private async Task<Guid> SetupSingleOrganisationForProject(Guid projectId, OrganisationRoleType role)
    {
        Guid orgId = Guid.NewGuid();

        Organisation org = new()
        {
            Id = orgId,
            Name = "Test Organisation",
            RORId = "https://ror.org/test",
        };

        _context.Organisations.Add(org);

        OrganisationRole orgRole = new()
        {
            ProjectId = projectId,
            OrganisationId = orgId,
            Role = role,
            StartDate = DateTime.UtcNow.Date,
        };

        _context.OrganisationRoles.Add(orgRole);

        ProjectOrganisation projectOrg = new()
        {
            ProjectId = projectId,
            OrganisationId = orgId,
            Roles = [orgRole],
        };

        _context.ProjectOrganisations.Add(projectOrg);
        await _context.SaveChangesAsync();
        return orgId;
    }

    private async Task<Guid> SetupOrganisationWithRoleHistory(Guid projectId)
    {
        Guid orgId = Guid.NewGuid();

        Organisation org = new()
        {
            Id = orgId,
            Name = "Test Organisation",
            RORId = "https://ror.org/test",
        };

        _context.Organisations.Add(org);

        // Create a role history: Funder -> Contractor (current)
        OrganisationRole endedFunderRole = new()
        {
            ProjectId = projectId,
            OrganisationId = orgId,
            Role = OrganisationRoleType.Funder,
            StartDate = DateTime.UtcNow.Date.AddDays(-30),
            EndDate = DateTime.UtcNow.Date.AddDays(-15),
        };

        OrganisationRole currentContractorRole = new()
        {
            ProjectId = projectId,
            OrganisationId = orgId,
            Role = OrganisationRoleType.Contractor,
            StartDate = DateTime.UtcNow.Date.AddDays(-15),
        };

        _context.OrganisationRoles.AddRange([endedFunderRole, currentContractorRole]);

        ProjectOrganisation projectOrg = new()
        {
            ProjectId = projectId,
            OrganisationId = orgId,
            Roles = [endedFunderRole, currentContractorRole],
        };

        _context.ProjectOrganisations.Add(projectOrg);
        await _context.SaveChangesAsync();
        return orgId;
    }

    #endregion
}
