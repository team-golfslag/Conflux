// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Conflux.Data;
using Conflux.Domain;
using Conflux.Domain.Logic.DTOs;
using Conflux.Domain.Logic.DTOs.Patch;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Conflux.API.Tests.Controllers;

public class ProjectsControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

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

        var projects = await response.Content.ReadFromJsonAsync<Project[]>();
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
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ConfluxContext context = scope.ServiceProvider.GetRequiredService<ConfluxContext>();
            project = await context.Projects.FindAsync(new Guid("00000000-0000-0000-0000-000000000002"));
        }

        // Then, update it
        ProjectDTO updatedProject = new()
        {
            Titles =
            [
                new()
                {
                    Text = "Updated Title",
                    Type = TitleType.Primary,
                    StartDate = new(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                },
            ],
            Description = "Updated description",
        };
        HttpResponseMessage putRes = await _client.PutAsJsonAsync($"/projects/{project!.Id}", updatedProject);
        putRes.EnsureSuccessStatusCode();

        Project? updated = await putRes.Content.ReadFromJsonAsync<Project>();
        Assert.NotNull(updated);
        Assert.Single(updated.Titles);
        Assert.Equal("Updated Title", updated.Titles[0].Text);
    }

    [Fact]
    public async Task PatchProject_UpdatesDescriptionOnly()
    {
        Project? project;
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ConfluxContext context = scope.ServiceProvider.GetRequiredService<ConfluxContext>();
            project = await context.Projects.FindAsync(new Guid("00000000-0000-0000-0000-000000000003"));
        }

        ProjectPatchDTO patchDto = new()
        {
            Description = "After patch",
        };
        HttpResponseMessage patchRes = await _client.PatchAsJsonAsync($"/projects/{project!.Id}", patchDto);
        patchRes.EnsureSuccessStatusCode();

        Project? updated = await patchRes.Content.ReadFromJsonAsync<Project>();
        Assert.Equal("After patch", updated?.Description);
    }

    [Fact]
    public async Task GetProjects_ByQuery_ReturnsMatchingProjects()
    {
        // Arrange: not needed, already seeded
        // Act
        HttpResponseMessage response = await _client.GetAsync("/projects?query=test");

        // Assert
        response.EnsureSuccessStatusCode();
        var projects = await response.Content.ReadFromJsonAsync<Project[]>();
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
        var projects = await response.Content.ReadFromJsonAsync<Project[]>();
        Assert.NotNull(projects);
        Assert.Contains(projects, p => p.Titles[0].Text == "Test Project");
    }

    [Fact]
    public async Task GetProjects_ByNonMatchingDateRange_ReturnsEmpty()
    {
        // Arrange
        ProjectDTO project = new()
        {
            Titles =
            [
                new()
                {
                    Text = "Outdated Project",
                    Type = TitleType.Primary,
                    StartDate = new(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                },
            ],
            Description = "Old project",
            StartDate = new(2010, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2010, 12, 31, 0, 0, 0, DateTimeKind.Utc),
        };
        JsonContent content = JsonContent.Create(project, options: new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        });
        await _client.PostAsync("/projects", content);

        // Act
        const string url = "/projects?query=outdated&start_date=2022-01-01T00:00:00Z&end_date=2022-12-31T23:59:59Z";
        HttpResponseMessage response = await _client.GetAsync(url);

        // Assert
        response.EnsureSuccessStatusCode();
        var projects = await response.Content.ReadFromJsonAsync<Project[]>();
        Assert.NotNull(projects);
        Assert.Empty(projects);
    }
}
