// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Conflux.Domain;
using Conflux.Domain.Logic.DTOs.Requests;
using Xunit;

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
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
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

        Project[]? projects = await response.Content.ReadFromJsonAsync<Project[]>(JsonOptions);
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
        ProjectRequestDTO updatedProjectRequest = new()
        {
            StartDate = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc),
        };

        HttpResponseMessage putRes =
            await _client.PutAsJsonAsync($"/projects/{new Guid("00000000-0000-0000-0000-000000000002")}",
                updatedProjectRequest, JsonOptions);
        putRes.EnsureSuccessStatusCode();

        Project? updated = await putRes.Content.ReadFromJsonAsync<Project>(JsonOptions);
        Assert.NotNull(updated);
        Assert.Equal(updatedProjectRequest.StartDate, updated.StartDate);
        Assert.Equal(updatedProjectRequest.EndDate, updated.EndDate);
    }

    [Fact]
    public async Task GetProjects_ByQuery_ReturnsMatchingProjects()
    {
        // Arrange: not needed, already seeded
        // Act
        HttpResponseMessage response = await _client.GetAsync("/projects?query=test");

        // Assert
        response.EnsureSuccessStatusCode();
        Project[]? projects = await response.Content.ReadFromJsonAsync<Project[]>(JsonOptions);
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
        Project[]? projects = await response.Content.ReadFromJsonAsync<Project[]>(JsonOptions);
        Assert.NotNull(projects);
        Assert.Contains(projects, p => p.Titles[0].Text == "Test Project");
    }

    [Fact]
    public async Task GetProjects_ByNonMatchingDateRange_ReturnsEmpty()
    {
        // Arrange
        ProjectRequestDTO projectRequest = new()
        {
            StartDate = new(2010, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new(2010, 12, 31, 0, 0, 0, DateTimeKind.Utc),
        };
        await _client.PostAsJsonAsync("/projects", projectRequest);

        // Act
        const string url = "/projects?query=outdated&start_date=2022-01-01T00:00:00Z&end_date=2022-12-31T23:59:59Z";
        HttpResponseMessage response = await _client.GetAsync(url);

        // Assert
        response.EnsureSuccessStatusCode();
        Project[]? projects = await response.Content.ReadFromJsonAsync<Project[]>(JsonOptions);
        Assert.NotNull(projects);
        Assert.Empty(projects);
    }
}
