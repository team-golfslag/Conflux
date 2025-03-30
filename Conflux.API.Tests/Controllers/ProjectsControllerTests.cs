using System.Net;
using System.Net.Http.Json;
using Conflux.Domain;
using Conflux.Domain.Logic.DTOs;
using Xunit;

namespace Conflux.API.Tests.Controllers;

public class ProjectsControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ProjectsControllerTests(TestWebApplicationFactory factory)
    {
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
    public async Task CreateProject_ReturnsCreated()
    {
        ProjectPostDTO newProject = new()
        {
            Title = "Integration Project",
            Description = "Test project",
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/projects", newProject);
        response.EnsureSuccessStatusCode();

        Project? created = await response.Content.ReadFromJsonAsync<Project>();
        Assert.Equal("Integration Project", created?.Title);
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
        ProjectPostDTO newProject = new()
        {
            Title = "Original Title",
            Description = "To be updated",
        };
        HttpResponseMessage createRes = await _client.PostAsJsonAsync("/projects", newProject);
        Project? created = await createRes.Content.ReadFromJsonAsync<Project>();

        // Then, update it
        ProjectPutDTO updatedProject = new()
        {
            Title = "Updated Title",
            Description = "Updated description",
        };
        HttpResponseMessage putRes = await _client.PutAsJsonAsync($"/projects/{created!.Id}", updatedProject);
        putRes.EnsureSuccessStatusCode();

        Project? updated = await putRes.Content.ReadFromJsonAsync<Project>();
        Assert.Equal("Updated Title", updated?.Title);
    }

    [Fact]
    public async Task PatchProject_UpdatesDescriptionOnly()
    {
        ProjectPostDTO newProject = new()
        {
            Title = "Patch Test",
            Description = "Before patch",
        };
        HttpResponseMessage createRes = await _client.PostAsJsonAsync("/projects", newProject);
        Project? created = await createRes.Content.ReadFromJsonAsync<Project>();

        ProjectPatchDTO patchDto = new()
        {
            Description = "After patch",
        };
        HttpResponseMessage patchRes = await _client.PatchAsJsonAsync($"/projects/{created!.Id}", patchDto);
        patchRes.EnsureSuccessStatusCode();

        Project? updated = await patchRes.Content.ReadFromJsonAsync<Project>();
        Assert.Equal("After patch", updated?.Description);
    }
}
