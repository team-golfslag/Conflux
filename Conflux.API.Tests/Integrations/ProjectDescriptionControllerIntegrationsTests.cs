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
using Conflux.Domain.Logic.DTOs.Responses;
using Xunit;

namespace Conflux.API.Tests.Integrations;

public class ProjectDescriptionsControllerIntegrationTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly Guid _existingProjectId = new("00000000-0000-0000-0000-000000000001");

    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters =
        {
            new JsonStringEnumConverter(),
        },
    };

    [Fact]
    public async Task GetDescriptions_ReturnsDescriptionsForExistingProject()
    {
        // First create a description to retrieve
        ProjectDescriptionRequestDTO descriptionDto = new()
        {
            Text = "Integration Test Description",
            Type = DescriptionType.Primary,
            Language = Language.ENGLISH,
        };

        HttpResponseMessage createResponse = await _client.PostAsJsonAsync(
            $"projects/{_existingProjectId}/descriptions",
            descriptionDto,
            _options);

        createResponse.EnsureSuccessStatusCode();

        // Act
        HttpResponseMessage response = await _client.GetAsync($"projects/{_existingProjectId}/descriptions");

        // Assert
        response.EnsureSuccessStatusCode();
        List<ProjectDescriptionResponseDTO>? descriptions = await response.Content
            .ReadFromJsonAsync<List<ProjectDescriptionResponseDTO>>(_options);

        Assert.NotNull(descriptions);
        Assert.Contains(descriptions, d => d.Text == "Integration Test Description");
    }

    [Fact]
    public async Task GetDescriptions_ReturnsNotFound_ForNonExistingProject()
    {
        // Arrange
        Guid nonExistingProjectId = Guid.NewGuid();

        // Act
        HttpResponseMessage response = await _client.GetAsync($"projects/{nonExistingProjectId}/descriptions");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetDescriptionById_ReturnsDescription_ForExistingDescription()
    {
        // Arrange - Create a description first
        ProjectDescriptionRequestDTO descriptionDto = new()
        {
            Text = "Get By Id Test Description",
            Type = DescriptionType.Brief,
            Language = Language.ENGLISH,
        };

        HttpResponseMessage createResponse = await _client.PostAsJsonAsync(
            $"projects/{_existingProjectId}/descriptions",
            descriptionDto,
            _options);

        createResponse.EnsureSuccessStatusCode();

        ProjectDescriptionResponseDTO? createdDescription = await createResponse.Content
            .ReadFromJsonAsync<ProjectDescriptionResponseDTO>(_options);
        Assert.NotNull(createdDescription);

        // Act
        HttpResponseMessage response = await _client.GetAsync(
            $"projects/{_existingProjectId}/descriptions/{createdDescription.Id}");

        // Assert
        response.EnsureSuccessStatusCode();
        ProjectDescriptionResponseDTO? description = await response.Content
            .ReadFromJsonAsync<ProjectDescriptionResponseDTO>(_options);

        Assert.NotNull(description);
        Assert.Equal(createdDescription.Id, description.Id);
        Assert.Equal("Get By Id Test Description", description.Text);
    }

    [Fact]
    public async Task GetDescriptionById_ReturnsNotFound_ForNonExistingDescription()
    {
        // Arrange
        Guid nonExistingDescriptionId = Guid.NewGuid();

        // Act
        HttpResponseMessage response = await _client.GetAsync(
            $"projects/{_existingProjectId}/descriptions/{nonExistingDescriptionId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateDescription_ReturnsCreatedDescription_WhenValid()
    {
        // Arrange
        ProjectDescriptionRequestDTO descriptionDto = new()
        {
            Text = "Create Test Description",
            Type = DescriptionType.Methods,
            Language = Language.ENGLISH,
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            $"projects/{_existingProjectId}/descriptions",
            descriptionDto,
            _options);

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        ProjectDescriptionResponseDTO? description = await response.Content
            .ReadFromJsonAsync<ProjectDescriptionResponseDTO>(_options);

        Assert.NotNull(description);
        Assert.Equal("Create Test Description", description.Text);
        Assert.Equal(DescriptionType.Methods, description.Type);
    }

    [Fact]
    public async Task CreateDescription_ReturnsNotFound_ForNonExistingProject()
    {
        // Arrange
        Guid nonExistingProjectId = Guid.NewGuid();
        ProjectDescriptionRequestDTO descriptionDto = new()
        {
            Text = "Test Description for Non-existing Project",
            Type = DescriptionType.Primary,
            Language = Language.ENGLISH,
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            $"projects/{nonExistingProjectId}/descriptions",
            descriptionDto,
            _options);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateDescription_ReturnsUpdatedDescription_WhenValid()
    {
        // Arrange - Create a description first
        ProjectDescriptionRequestDTO createDto = new()
        {
            Text = "Original Description",
            Type = DescriptionType.Primary,
            Language = Language.ENGLISH,
        };

        HttpResponseMessage createResponse = await _client.PostAsJsonAsync(
            $"projects/{_existingProjectId}/descriptions",
            createDto,
            _options);

        createResponse.EnsureSuccessStatusCode();

        ProjectDescriptionResponseDTO? createdDescription = await createResponse.Content
            .ReadFromJsonAsync<ProjectDescriptionResponseDTO>(_options);
        Assert.NotNull(createdDescription);

        // Now update it
        ProjectDescriptionRequestDTO updateDto = new()
        {
            Text = "Updated Description",
            Type = DescriptionType.Alternative,
            Language = Language.DUTCH,
        };

        // Act
        HttpResponseMessage response = await _client.PutAsJsonAsync(
            $"projects/{_existingProjectId}/descriptions/{createdDescription.Id}",
            updateDto,
            _options);

        // Assert
        response.EnsureSuccessStatusCode();
        ProjectDescriptionResponseDTO? updatedDescription = await response.Content
            .ReadFromJsonAsync<ProjectDescriptionResponseDTO>(_options);

        Assert.NotNull(updatedDescription);
        Assert.Equal(createdDescription.Id, updatedDescription.Id);
        Assert.Equal("Updated Description", updatedDescription.Text);
        Assert.Equal(DescriptionType.Alternative, updatedDescription.Type);
        Assert.Equal(Language.DUTCH.Id, updatedDescription.Language.Id);
    }

    [Fact]
    public async Task UpdateDescription_ReturnsNotFound_ForNonExistingDescription()
    {
        // Arrange
        Guid nonExistingDescriptionId = Guid.NewGuid();
        ProjectDescriptionRequestDTO updateDto = new()
        {
            Text = "Update Non-existing Description",
            Type = DescriptionType.Primary,
            Language = Language.ENGLISH,
        };

        // Act
        HttpResponseMessage response = await _client.PutAsJsonAsync(
            $"projects/{_existingProjectId}/descriptions/{nonExistingDescriptionId}",
            updateDto,
            _options);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteDescription_ReturnsNoContent_WhenSuccessful()
    {
        // Arrange - Create a description first
        ProjectDescriptionRequestDTO createDto = new()
        {
            Text = "Description to Delete",
            Type = DescriptionType.Primary,
            Language = Language.ENGLISH,
        };

        HttpResponseMessage createResponse = await _client.PostAsJsonAsync(
            $"projects/{_existingProjectId}/descriptions",
            createDto,
            _options);

        createResponse.EnsureSuccessStatusCode();

        ProjectDescriptionResponseDTO? createdDescription = await createResponse.Content
            .ReadFromJsonAsync<ProjectDescriptionResponseDTO>(_options);
        Assert.NotNull(createdDescription);

        // Act
        HttpResponseMessage response = await _client.DeleteAsync(
            $"projects/{_existingProjectId}/descriptions/{createdDescription.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify it's gone
        HttpResponseMessage getResponse = await _client.GetAsync(
            $"projects/{_existingProjectId}/descriptions/{createdDescription.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteDescription_ReturnsNotFound_ForNonExistingDescription()
    {
        // Arrange
        Guid nonExistingDescriptionId = Guid.NewGuid();

        // Act
        HttpResponseMessage response = await _client.DeleteAsync(
            $"projects/{_existingProjectId}/descriptions/{nonExistingDescriptionId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
