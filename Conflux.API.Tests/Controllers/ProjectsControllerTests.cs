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
using Conflux.Domain.Logic.DTOs;
using Conflux.Domain.Logic.DTOs.Patch;
using Xunit;
using IServiceScope = Microsoft.Extensions.DependencyInjection.IServiceScope;
using ServiceProviderServiceExtensions = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions;

namespace Conflux.API.Tests.Controllers;

public class ProjectsControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions;
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    static ProjectsControllerTests()
    {
        JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };
        // allow "Primary", "Secondary", etc. to bind into TitleType/DescriptionType enum properties
        JsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public ProjectsControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAllProjects_ReturnsSuccess()
    {
        HttpResponseMessage response = await _client.GetAsync("/projects/all");
        response.EnsureSuccessStatusCode();

        var projects = await response.Content.ReadFromJsonAsync<Project[]>(JsonOptions);
        Assert.NotNull(projects);
    }

    [Fact]
    public async Task GetProjectById_ReturnsNotFound_ForInvalidId()
    {
        Guid invalidId = Guid.NewGuid();
        HttpResponseMessage response = await _client.GetAsync($"/projects/{invalidId}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PutProject_UpdatesProject()
    {
        // First, create a new project
        Project? project;
        using (IServiceScope scope = ServiceProviderServiceExtensions.CreateScope(_factory.Services))
        {
            ConfluxContext context =
                ServiceProviderServiceExtensions.GetRequiredService<ConfluxContext>(scope.ServiceProvider);
            project = await context.Projects.FindAsync(new Guid("00000000-0000-0000-0000-000000000002"));
        }

        // Then, update it
        ProjectRequestDTO updatedProjectRequest = new()
        {
            Id = project!.Id,
            Titles =
            [
                new()
                {
                    Text = "Updated Title",
                    Type = TitleType.Primary,
                    StartDate = new(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                },
            ],
            Descriptions =
            [
                new()
                {
                    Text = "Updated description",
                    Type = DescriptionType.Primary,
                    Language = Language.ENGLISH,
                },
            ],
        };
        HttpResponseMessage putRes = await _client.PutAsJsonAsync($"/projects/{project!.Id}", updatedProjectRequest);
        putRes.EnsureSuccessStatusCode();

        Project? updated = await putRes.Content.ReadFromJsonAsync<Project>(JsonOptions);
        Assert.NotNull(updated);
        Assert.Single(updated.Titles);
        Assert.Equal("Updated Title", updated.Titles[0].Text);
    }

    [Fact]
    public async Task PatchProject_UpdatesDescriptionOnly()
    {
        Project? project;
        using (IServiceScope scope = ServiceProviderServiceExtensions.CreateScope(_factory.Services))
        {
            ConfluxContext context =
                ServiceProviderServiceExtensions.GetRequiredService<ConfluxContext>(scope.ServiceProvider);
            project = await context.Projects.FindAsync(new Guid("00000000-0000-0000-0000-000000000003"));
        }

        ProjectPatchDTO patchDto = new()
        {
            Descriptions =
            [
                new()
                {
                    Text = "After patch",
                    Type = DescriptionType.Primary,
                    Language = Language.ENGLISH,
                },
            ],
        };
        HttpResponseMessage patchRes = await _client.PatchAsJsonAsync($"/projects/{project!.Id}", patchDto);
        patchRes.EnsureSuccessStatusCode();

        Project? updated = await patchRes.Content.ReadFromJsonAsync<Project>(JsonOptions);
        Assert.NotNull(updated);
        Assert.Single(updated.Descriptions);
        Assert.Equal("After patch", updated.Descriptions[0].Text);
    }

    [Fact]
    public async Task GetProjects_ByQuery_ReturnsMatchingProjects()
    {
        // Arrange: not needed, already seeded
        // Act
        HttpResponseMessage response = await _client.GetAsync("/projects?query=test");

        // Assert
        response.EnsureSuccessStatusCode();
        var projects = await response.Content.ReadFromJsonAsync<Project[]>(JsonOptions);
        Assert.NotNull(projects);
        Assert.Contains(projects, p => p.Titles[0].Text.Contains("Test Project"));
    }

    [Fact]
    public async Task GetProjects_ByDateRange_ReturnsMatchingProjects()
    {
        // Arrange already seeded
        // Act
        const string url = "/projects?query=test&start_date=2024-01-01T00:00:00Z&end_date=2024-12-31T23:59:59Z";
        HttpResponseMessage response = await _client.GetAsync(url);

        // Assert
        response.EnsureSuccessStatusCode();
        var projects = await response.Content.ReadFromJsonAsync<Project[]>(JsonOptions);
        Assert.NotNull(projects);
        Assert.Contains(projects, p => p.Titles[0].Text == "Test Project");
    }

    [Fact]
    public async Task GetProjects_ByNonMatchingDateRange_ReturnsEmpty()
    {
        // Arrange
        ProjectRequestDTO projectRequest = new()
        {
            Id = Guid.NewGuid(),
            Titles =
            [
                new()
                {
                    Text = "Outdated Project",
                    Type = TitleType.Primary,
                    StartDate = new(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                },
            ],
            Descriptions =
            [
                new()
                {
                    Text = "Old project",
                    Type = DescriptionType.Primary,
                    Language = Language.ENGLISH,
                },
            ],
            StartDate = new(2010, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2010, 12, 31, 0, 0, 0, DateTimeKind.Utc),
        };
        JsonContent content = JsonContent.Create(projectRequest, options: new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        });
        await _client.PostAsync("/projects", content);

        // Act
        const string url = "/projects?query=outdated&start_date=2022-01-01T00:00:00Z&end_date=2022-12-31T23:59:59Z";
        HttpResponseMessage response = await _client.GetAsync(url);

        // Assert
        response.EnsureSuccessStatusCode();
        var projects = await response.Content.ReadFromJsonAsync<Project[]>(JsonOptions);
        Assert.NotNull(projects);
        Assert.Empty(projects);
    }
}
