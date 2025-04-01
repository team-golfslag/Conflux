using System.Net;
using System.Net.Http.Json;
using Conflux.Domain;
using Conflux.Domain.Logic.DTOs;
using Xunit;

namespace Conflux.API.Tests.Controllers;

public class PeopleControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PeopleControllerTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreatePerson_ReturnsCreatedPerson()
    {
        // Arrange
        PersonPostDTO newPerson = new()
        {
            Name = "Test Person",
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/people", newPerson);
        response.EnsureSuccessStatusCode();

        Person? createdPerson = await response.Content.ReadFromJsonAsync<Person>();

        // Assert
        Assert.NotNull(createdPerson);
        Assert.Equal("Test Person", createdPerson.Name);
    }

    [Fact]
    public async Task GetPersonById_ReturnsPerson_WhenExists()
    {
        // Arrange: create a person first
        PersonPostDTO newPerson = new()
        {
            Name = "John Doe",
        };

        HttpResponseMessage createRes = await _client.PostAsJsonAsync("/people", newPerson);
        createRes.EnsureSuccessStatusCode();
        Person? createdPerson = await createRes.Content.ReadFromJsonAsync<Person>();
        Assert.NotNull(createdPerson);

        // Act
        HttpResponseMessage getRes = await _client.GetAsync($"/people/{createdPerson.Id}");
        getRes.EnsureSuccessStatusCode();
        Person? fetchedPerson = await getRes.Content.ReadFromJsonAsync<Person>();

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
        PersonPostDTO newPerson = new()
        {
            Name = "Original Name",
        };

        HttpResponseMessage createRes = await _client.PostAsJsonAsync("/people", newPerson);
        createRes.EnsureSuccessStatusCode();
        Person? createdPerson = await createRes.Content.ReadFromJsonAsync<Person>();
        Assert.NotNull(createdPerson);

        // Prepare update DTO
        PersonPutDTO updateDto = new()
        {
            Name = "Updated Name",
        };

        // Act: update the person (PUT /person/{id})
        HttpResponseMessage putRes = await _client.PutAsJsonAsync($"/people/{createdPerson.Id}", updateDto);
        putRes.EnsureSuccessStatusCode();

        // Retrieve updated person
        HttpResponseMessage getRes = await _client.GetAsync($"/people/{createdPerson.Id}");
        getRes.EnsureSuccessStatusCode();
        Person? updatedPerson = await getRes.Content.ReadFromJsonAsync<Person>();

        // Assert
        Assert.NotNull(updatedPerson);
        Assert.Equal("Updated Name", updatedPerson!.Name);
    }

    [Fact]
    public async Task PatchPerson_PatchesPersonSuccessfully()
    {
        // Arrange: create a person first
        PersonPostDTO newPerson = new()
        {
            Name = "Initial Name",
        };

        HttpResponseMessage createRes = await _client.PostAsJsonAsync("/people", newPerson);
        createRes.EnsureSuccessStatusCode();
        Person? createdPerson = await createRes.Content.ReadFromJsonAsync<Person>();
        Assert.NotNull(createdPerson);

        // Prepare patch DTO to change the name
        PersonPatchDTO patchDto = new()
        {
            Name = "Patched Name",
        };

        // Act: patch the person (PATCH /person/{id})
        HttpResponseMessage patchRes = await _client.PatchAsJsonAsync($"/people/{createdPerson.Id}", patchDto);
        patchRes.EnsureSuccessStatusCode();

        // Retrieve patched person
        HttpResponseMessage getRes = await _client.GetAsync($"/people/{createdPerson.Id}");
        getRes.EnsureSuccessStatusCode();
        Person? patchedPerson = await getRes.Content.ReadFromJsonAsync<Person>();

        // Assert
        Assert.NotNull(patchedPerson);
        Assert.Equal("Patched Name", patchedPerson!.Name);
    }
}
