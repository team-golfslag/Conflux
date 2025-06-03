// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Conflux.Data;
using Conflux.Domain;
using Conflux.Domain.Logic.DTOs.Requests;
using Conflux.Domain.Logic.DTOs.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Conflux.API.Tests.Integrations;

public class ProjectOrganisationsControllerIntegrationTests
    : IClassFixture<WebApplicationFactoryTests>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactoryTests _factory;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters =
        {
            new JsonStringEnumConverter(),
        },
    };

    public ProjectOrganisationsControllerIntegrationTests(WebApplicationFactoryTests factory)
    {
        _factory = factory;
        _client = factory.CreateClient(
            new()
            {
                AllowAutoRedirect = false,
            });
    }

    [Fact]
    public async Task GetOrganisations_ReturnsOkWithOrganisations_WhenProjectExists()
    {
        // Arrange
        using IServiceScope scope = _factory.Services.CreateScope();
        ConfluxContext context = scope.ServiceProvider.GetRequiredService<ConfluxContext>();

        Guid projectId = Guid.NewGuid();
        Project project = new()
        {
            Id = projectId,
            StartDate = DateTime.UtcNow,
            SCIMId = "SCIM",
            Titles =
            [
                new()
                {
                    ProjectId = projectId,
                    Text = "Test Project",
                    Type = TitleType.Primary,
                    StartDate = DateTime.UtcNow,
                },
            ],
        };

        Guid orgId = Guid.NewGuid();
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
                    Role = OrganisationRoleType.Contractor,
                },
            ],
        };

        context.Projects.Add(project);
        context.Organisations.Add(org);
        context.ProjectOrganisations.Add(projectOrg);
        await context.SaveChangesAsync();

        // Act
        HttpResponseMessage response = await _client.GetAsync($"projects/{projectId}/organisations");

        // Assert
        response.EnsureSuccessStatusCode();
        List<ProjectOrganisationResponseDTO>? organisations =
            await response.Content.ReadFromJsonAsync<List<ProjectOrganisationResponseDTO>>(_jsonOptions);

        Assert.NotNull(organisations);
        Assert.Single(organisations);
        Assert.Equal(orgId, organisations[0].Organisation.Id);
        Assert.Equal("Test Organisation", organisations[0].Organisation.Name);
        Assert.Equal("https://ror.org/test", organisations[0].Organisation.RORId);
        Assert.Single(organisations[0].Organisation.Roles);
        Assert.Equal(OrganisationRoleType.Contractor, organisations[0].Organisation.Roles[0].Role);
    }

    [Fact]
    public async Task GetOrganisations_ReturnsNotFound_WhenProjectDoesNotExist()
    {
        // Act
        HttpResponseMessage response = await _client.GetAsync($"projects/{Guid.NewGuid()}/organisations");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetOrganisationById_ReturnsOkWithOrganisation_WhenExists()
    {
        // Arrange
        using IServiceScope scope = _factory.Services.CreateScope();
        ConfluxContext context = scope.ServiceProvider.GetRequiredService<ConfluxContext>();

        Guid projectId = Guid.NewGuid();
        Project project = new()
        {
            Id = projectId,
            StartDate = DateTime.UtcNow,
            SCIMId = "SCIM",
            Titles =
            [
                new()
                {
                    ProjectId = projectId,
                    Text = "Test Project",
                    Type = TitleType.Primary,
                    StartDate = DateTime.UtcNow,
                },
            ],
        };

        Guid orgId = Guid.NewGuid();
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

        context.Projects.Add(project);
        context.Organisations.Add(org);
        context.ProjectOrganisations.Add(projectOrg);
        await context.SaveChangesAsync();

        // Act
        HttpResponseMessage response = await _client.GetAsync($"projects/{projectId}/organisations/{orgId}");

        // Assert
        response.EnsureSuccessStatusCode();
        ProjectOrganisationResponseDTO? organisation =
            await response.Content.ReadFromJsonAsync<ProjectOrganisationResponseDTO>(_jsonOptions);

        Assert.NotNull(organisation);
        Assert.Equal(orgId, organisation.Organisation.Id);
        Assert.Equal("Test Organisation", organisation.Organisation.Name);
        Assert.Equal("https://ror.org/test", organisation.Organisation.RORId);
        Assert.Single(organisation.Organisation.Roles);
        Assert.Equal(OrganisationRoleType.Funder, organisation.Organisation.Roles[0].Role);
    }

    [Fact]
    public async Task GetOrganisationById_ReturnsNotFound_WhenProjectOrganisationDoesNotExist()
    {
        // Arrange
        using IServiceScope scope = _factory.Services.CreateScope();
        ConfluxContext context = scope.ServiceProvider.GetRequiredService<ConfluxContext>();

        Guid projectId = Guid.NewGuid();
        Project project = new()
        {
            Id = projectId,
            StartDate = DateTime.UtcNow,
            Titles =
            [
                new()
                {
                    ProjectId = projectId,
                    Text = "Test Project",
                    Type = TitleType.Primary,
                    StartDate = DateTime.UtcNow,
                },
            ],
        };
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        // Act
        HttpResponseMessage response = await _client.GetAsync($"projects/{projectId}/organisations/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrganisation_ReturnsCreatedWithOrganisation_WhenValid()
    {
        // Arrange
        using IServiceScope scope = _factory.Services.CreateScope();
        ConfluxContext context = scope.ServiceProvider.GetRequiredService<ConfluxContext>();

        Guid projectId = Guid.NewGuid();
        Project project = new()
        {
            Id = projectId,
            SCIMId = "SCIM",
            StartDate = DateTime.UtcNow,
            Titles =
            [
                new()
                {
                    ProjectId = projectId,
                    Text = "Test Project",
                    Type = TitleType.Primary,
                    StartDate = DateTime.UtcNow,
                },
            ],
        };
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        OrganisationRequestDTO newOrgDto = new()
        {
            Name = "New Organisation",
            RORId = "https://ror.org/new",
            Role = OrganisationRoleType.Contractor,
        };

        // Act
        HttpResponseMessage response =
            await _client.PostAsJsonAsync($"projects/{projectId}/organisations", newOrgDto, _jsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        ProjectOrganisationResponseDTO? createdOrg =
            await response.Content.ReadFromJsonAsync<ProjectOrganisationResponseDTO>(_jsonOptions);

        Assert.NotNull(createdOrg);
        Assert.Equal("New Organisation", createdOrg.Organisation.Name);
        Assert.Equal("https://ror.org/new", createdOrg.Organisation.RORId);
        Assert.Single(createdOrg.Organisation.Roles);
        Assert.Equal(OrganisationRoleType.Contractor, createdOrg.Organisation.Roles[0].Role);

        // Check the organization was added to the database
        bool orgExists = await context.Organisations.AnyAsync(o => o.Name == "New Organisation");
        Assert.True(orgExists);
    }

    [Fact]
    public async Task CreateOrganisation_ReturnsNotFound_WhenProjectDoesNotExist()
    {
        // Arrange
        OrganisationRequestDTO newOrgDto = new()
        {
            Name = "New Organisation",
            RORId = "https://ror.org/new",
            Role = null,
        };

        // Act
        HttpResponseMessage response =
            await _client.PostAsJsonAsync($"projects/{Guid.NewGuid()}/organisations", newOrgDto);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact(Skip = "Cannot delete as of now since the function does not work with in memory database")]
    public async Task UpdateOrganisation_ReturnsOkWithUpdatedOrganisation_WhenValid()
    {
        // Arrange
        using IServiceScope scope = _factory.Services.CreateScope();
        ConfluxContext context = scope.ServiceProvider.GetRequiredService<ConfluxContext>();

        Guid projectId = Guid.NewGuid();
        Project project = new()
        {
            Id = projectId,
            StartDate = DateTime.UtcNow,
            SCIMId = "SCIM",
            Titles =
            [
                new()
                {
                    ProjectId = projectId,
                    Text = "Test Project",
                    Type = TitleType.Primary,
                    StartDate = DateTime.UtcNow,
                },
            ],
        };

        Guid orgId = Guid.NewGuid();
        Organisation org = new()
        {
            Id = orgId,
            Name = "Original Name",
            RORId = "https://ror.org/original",
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
                    Role = OrganisationRoleType.Facility,
                },
            ],
        };

        context.Projects.Add(project);
        context.Organisations.Add(org);
        context.ProjectOrganisations.Add(projectOrg);
        await context.SaveChangesAsync();

        OrganisationRequestDTO updateDto = new()
        {
            Name = "Updated Name",
            RORId = "https://ror.org/updated",
            Role = OrganisationRoleType.Funder,
        };

        // Act
        HttpResponseMessage response =
            await _client.PutAsJsonAsync($"projects/{projectId}/organisations/{orgId}", updateDto, _jsonOptions);

        // Assert
        response.EnsureSuccessStatusCode();
        ProjectOrganisationResponseDTO? updatedOrg =
            await response.Content.ReadFromJsonAsync<ProjectOrganisationResponseDTO>(_jsonOptions);

        Assert.NotNull(updatedOrg);
        Assert.Equal("Updated Name", updatedOrg.Organisation.Name);
        Assert.Equal("https://ror.org/updated", updatedOrg.Organisation.RORId);
        Assert.Single(updatedOrg.Organisation.Roles);
        Assert.Equal(OrganisationRoleType.Funder, updatedOrg.Organisation.Roles[0].Role);
    }

    [Fact]
    public async Task UpdateOrganisation_ReturnsNotFound_WhenOrganisationDoesNotExist()
    {
        // Arrange
        using IServiceScope scope = _factory.Services.CreateScope();
        ConfluxContext context = scope.ServiceProvider.GetRequiredService<ConfluxContext>();

        Guid projectId = Guid.NewGuid();
        Project project = new()
        {
            Id = projectId,
            StartDate = DateTime.UtcNow,
            Titles =
            [
                new()
                {
                    ProjectId = projectId,
                    Text = "Test Project",
                    Type = TitleType.Primary,
                    StartDate = DateTime.UtcNow,
                },
            ],
        };
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        OrganisationRequestDTO updateDto = new()
        {
            Name = "Updated Name",
            RORId = "https://ror.org/updated",
            Role = null,
        };

        // Act
        HttpResponseMessage response =
            await _client.PutAsJsonAsync($"projects/{projectId}/organisations/{Guid.NewGuid()}", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact(Skip = "Cannot delete as of now since the function does not work with in memory database")]
    public async Task DeleteOrganisation_ReturnsNoContent_WhenOrganisationExists()
    {
        // Arrange
        using IServiceScope scope = _factory.Services.CreateScope();
        ConfluxContext context = scope.ServiceProvider.GetRequiredService<ConfluxContext>();

        Guid projectId = Guid.NewGuid();
        Project project = new()
        {
            Id = projectId,
            StartDate = DateTime.UtcNow,
            Titles =
            [
                new()
                {
                    ProjectId = projectId,
                    Text = "Test Project",
                    Type = TitleType.Primary,
                    StartDate = DateTime.UtcNow,
                },
            ],
        };

        Guid orgId = Guid.NewGuid();
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
                    Role = OrganisationRoleType.Contractor,
                },
            ],
        };

        context.Projects.Add(project);
        context.Organisations.Add(org);
        context.ProjectOrganisations.Add(projectOrg);
        await context.SaveChangesAsync();

        // Act
        HttpResponseMessage response = await _client.DeleteAsync($"projects/{projectId}/organisations/{orgId}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify the relationship was deleted
        bool relationshipExists = await context.ProjectOrganisations
            .AnyAsync(po => po.ProjectId == projectId && po.OrganisationId == orgId);
        Assert.False(relationshipExists);

        // Verify the organisation still exists (only the relationship should be deleted)
        bool orgExists = await context.Organisations.AnyAsync(o => o.Id == orgId);
        Assert.True(orgExists);
    }

    [Fact(Skip = "Cannot delete as of now since the function does not work with in memory database")]
    public async Task DeleteOrganisation_ReturnsNotFound_WhenProjectOrganisationDoesNotExist()
    {
        // Arrange
        using IServiceScope scope = _factory.Services.CreateScope();
        ConfluxContext context = scope.ServiceProvider.GetRequiredService<ConfluxContext>();

        Guid projectId = Guid.NewGuid();
        Project project = new()
        {
            Id = projectId,
            StartDate = DateTime.UtcNow,
            Titles =
            [
                new()
                {
                    ProjectId = projectId,
                    Text = "Test Project",
                    Type = TitleType.Primary,
                    StartDate = DateTime.UtcNow,
                },
            ],
        };
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        // Act
        HttpResponseMessage response =
            await _client.DeleteAsync($"projects/{projectId}/organisations/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
