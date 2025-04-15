// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Net;
using System.Net.Http.Json;
using Conflux.Domain;
using Conflux.Domain.Logic.DTOs;
using Xunit;

namespace Conflux.API.Tests.Controllers;

public class ContributorsControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ContributorsControllerTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetPeople_ByQuery_ReturnsMatchingPeople()
    {
        // Arrange
        ContributorPostDto contributor = new()
        {
            Name = "Stefan Herald",
        };
        await _client.PostAsJsonAsync("/people", contributor);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/people?query=stefan");

        // Assert
        response.EnsureSuccessStatusCode();
        var people = await response.Content.ReadFromJsonAsync<List<Contributor>>();
        Assert.NotNull(people);
        Assert.Contains(people, p => p.Name.Equals("Stefan Herald"));
    }

    [Fact]
    public async Task CreatePerson_ReturnsCreatedPerson()
    {
        // Arrange
        ContributorPostDto newContributor = new()
        {
            Name = "Test Person",
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/people", newContributor);
        response.EnsureSuccessStatusCode();

        Contributor? createdPerson = await response.Content.ReadFromJsonAsync<Contributor>();

        // Assert
        Assert.NotNull(createdPerson);
        Assert.Equal("Test Person", createdPerson.Name);
    }

    [Fact]
    public async Task GetPersonById_ReturnsPerson_WhenExists()
    {
        // Arrange: create a person first
        ContributorPostDto newContributor = new()
        {
            Name = "John Doe",
        };

        HttpResponseMessage createRes = await _client.PostAsJsonAsync("/people", newContributor);
        createRes.EnsureSuccessStatusCode();
        Contributor? createdPerson = await createRes.Content.ReadFromJsonAsync<Contributor>();
        Assert.NotNull(createdPerson);

        // Act
        HttpResponseMessage getRes = await _client.GetAsync($"/people/{createdPerson.Id}");
        getRes.EnsureSuccessStatusCode();
        Contributor? fetchedPerson = await getRes.Content.ReadFromJsonAsync<Contributor>();

        // Assert
        Assert.NotNull(fetchedPerson);
        Assert.Equal(createdPerson.Id, fetchedPerson.Id);
        Assert.Equal("John Doe", fetchedPerson.Name);
    }

    [Fact]
    public async Task GetPersonById_ReturnsNotFound_WhenPersonDoesNotExist()
    {
        // Arrange
        Guid invalidId = Guid.NewGuid();

        // Act
        HttpResponseMessage getRes = await _client.GetAsync($"/people/{invalidId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, getRes.StatusCode);
    }

    [Fact]
    public async Task UpdatePerson_UpdatesPersonSuccessfully()
    {
        // Arrange: create a person first
        ContributorPostDto newContributor = new()
        {
            Name = "Original Name",
        };

        HttpResponseMessage createRes = await _client.PostAsJsonAsync("/people", newContributor);
        createRes.EnsureSuccessStatusCode();
        Contributor? createdPerson = await createRes.Content.ReadFromJsonAsync<Contributor>();
        Assert.NotNull(createdPerson);

        // Prepare update DTO
        ContributorPutDto updateDto = new()
        {
            Name = "Updated Name",
        };

        // Act: update the person (PUT /person/{id})
        HttpResponseMessage putRes = await _client.PutAsJsonAsync($"/people/{createdPerson.Id}", updateDto);
        putRes.EnsureSuccessStatusCode();

        // Retrieve updated person
        HttpResponseMessage getRes = await _client.GetAsync($"/people/{createdPerson.Id}");
        getRes.EnsureSuccessStatusCode();
        Contributor? updatedPerson = await getRes.Content.ReadFromJsonAsync<Contributor>();

        // Assert
        Assert.NotNull(updatedPerson);
        Assert.Equal("Updated Name", updatedPerson!.Name);
    }

    [Fact]
    public async Task PatchPerson_PatchesPersonSuccessfully()
    {
        // Arrange: create a person first
        ContributorPostDto newContributor = new()
        {
            Name = "Initial Name",
        };

        HttpResponseMessage createRes = await _client.PostAsJsonAsync("/people", newContributor);
        createRes.EnsureSuccessStatusCode();
        Contributor? createdPerson = await createRes.Content.ReadFromJsonAsync<Contributor>();
        Assert.NotNull(createdPerson);

        // Prepare patch DTO to change the name
        ContributorPatchDto patchDto = new()
        {
            Name = "Patched Name",
        };

        // Act: patch the person (PATCH /person/{id})
        HttpResponseMessage patchRes = await _client.PatchAsJsonAsync($"/people/{createdPerson.Id}", patchDto);
        patchRes.EnsureSuccessStatusCode();

        // Retrieve patched person
        HttpResponseMessage getRes = await _client.GetAsync($"/people/{createdPerson.Id}");
        getRes.EnsureSuccessStatusCode();
        Contributor? patchedPerson = await getRes.Content.ReadFromJsonAsync<Contributor>();

        // Assert
        Assert.NotNull(patchedPerson);
        Assert.Equal("Patched Name", patchedPerson!.Name);
    }
}
