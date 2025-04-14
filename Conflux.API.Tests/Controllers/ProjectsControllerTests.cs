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

    [Fact]
    public async Task GetProjects_ByQuery_ReturnsMatchingProjects()
    {
        // Arrange
        ProjectPostDTO project = new()
        {
            Title = "Solar Panel Research",
            Description = "Test description",
        };
        await _client.PostAsJsonAsync("/projects", project);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/projects?query=solar");

        // Assert
        response.EnsureSuccessStatusCode();
        var projects = await response.Content.ReadFromJsonAsync<Project[]>();
        Assert.NotNull(projects);
        Assert.Contains(projects, p => p.Title.Contains("Solar Panel Research"));
    }

    [Fact]
    public async Task GetProjects_ByDateRange_ReturnsMatchingProjects()
    {
        // Arrange
        ProjectPostDTO project = new()
        {
            Title = "Dated Project",
            Description = "Project with specific dates",
            StartDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc),
        };
        JsonContent content = JsonContent.Create(project, options: new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        });
        await _client.PostAsync("/projects", content);

        // Act
        const string url = "/projects?query=dated&start_date=2024-01-01T00:00:00Z&end_date=2024-12-31T23:59:59Z";
        HttpResponseMessage response = await _client.GetAsync(url);

        // Assert
        response.EnsureSuccessStatusCode();
        var projects = await response.Content.ReadFromJsonAsync<Project[]>();
        Assert.NotNull(projects);
        Assert.Contains(projects, p => p.Title == "Dated Project");
    }

    [Fact]
    public async Task GetProjects_ByNonMatchingDateRange_ReturnsEmpty()
    {
        // Arrange
        ProjectPostDTO project = new()
        {
            Title = "Outdated Project",
            Description = "Old project",
            StartDate = new DateTime(2010, 1, 1, 0, 0, 0, DateTimeKind.Utc),
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

    [Fact]
    public async Task AddPersonToProjectAsync_AddsPersonSuccessfully()
    {
        // Arrange: Create a new project using the API
        ProjectPostDTO newProject = new()
        {
            Title = "Test Project",
            Description = "Project for add person testing",
        };
        HttpResponseMessage createProjectRes = await _client.PostAsJsonAsync("/projects", newProject);
        createProjectRes.EnsureSuccessStatusCode();
        Project? project = await createProjectRes.Content.ReadFromJsonAsync<Project>();
        Assert.NotNull(project);

        // Arrange: Create a new person by seeding the database
        Guid personId = Guid.NewGuid();
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ConfluxContext context = scope.ServiceProvider.GetRequiredService<ConfluxContext>();
            Contributor contributor = new()
            {
                Id = personId,
                Name = "Test Person",
            };
            context.Contributors.Add(contributor);
            await context.SaveChangesAsync();
        }

        // Act: Call the AddPersonToProjectAsync endpoint
        HttpResponseMessage addRes = await _client.PostAsync($"/projects/{project!.Id}/addPerson/{personId}", null);
        addRes.EnsureSuccessStatusCode();
        Project? updatedProject = await addRes.Content.ReadFromJsonAsync<Project>();

        // Assert: The updated project should contain the person
        Assert.NotNull(updatedProject);
        Assert.NotNull(updatedProject!.Contributors);
        Assert.Contains(updatedProject.Contributors, p => p.Id == personId);
    }

    [Fact]
    public async Task AddPersonToProjectAsync_ReturnsNotFound_WhenProjectDoesNotExist()
    {
        // Arrange: Create a person in the database
        Guid personId = Guid.NewGuid();
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ConfluxContext context = scope.ServiceProvider.GetRequiredService<ConfluxContext>();
            Contributor contributor = new()
            {
                Id = personId,
                Name = "Test Person",
            };
            context.Contributors.Add(contributor);
            await context.SaveChangesAsync();
        }

        // Act: Use a non-existent project ID
        Guid nonExistentProjectId = Guid.NewGuid();
        HttpResponseMessage response =
            await _client.PostAsync($"/projects/{nonExistentProjectId}/addPerson/{personId}", null);

        // Assert: The response should be NotFound
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AddPersonToProjectAsync_ReturnsNotFound_WhenPersonDoesNotExist()
    {
        // Arrange: Create a new project using the API
        ProjectPostDTO newProject = new()
        {
            Title = "Test Project",
            Description = "Project for add person testing",
        };
        HttpResponseMessage createProjectRes = await _client.PostAsJsonAsync("/projects", newProject);
        createProjectRes.EnsureSuccessStatusCode();
        Project? project = await createProjectRes.Content.ReadFromJsonAsync<Project>();
        Assert.NotNull(project);

        // Act: Use a non-existent person ID
        Guid nonExistentPersonId = Guid.NewGuid();
        HttpResponseMessage response =
            await _client.PostAsync($"/projects/{project!.Id}/addPerson/{nonExistentPersonId}", null);

        // Assert: The response should be NotFound
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
