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

public class PeopleControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PeopleControllerTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
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
}
