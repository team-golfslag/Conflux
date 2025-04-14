// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)


using System.Net;
using Conflux.Integrations.SRAM.Tests.Helpers;
using Conflux.RepositoryConnections.SRAM;
using Conflux.RepositoryConnections.SRAM.Models;
using Xunit;

namespace Conflux.Integrations.SRAM.Tests;

public class SCIMApiClientTests
{
    [Fact]
    public void SetBearerToken_SetsAuthorizationHeader()
    {
        // Arrange
        string token = "VeryValidToken";
        FakeHttpMessageHandler handler = new("", HttpStatusCode.OK);
        HttpClient httpClient = new(handler)
        {
            BaseAddress = new("https://dummy/"),
        };
        SCIMApiClient client = new(httpClient);

        // Act
        client.SetBearerToken(token);

        // Assert
        Assert.NotNull(httpClient.DefaultRequestHeaders.Authorization);
        Assert.Equal("Bearer", httpClient.DefaultRequestHeaders.Authorization.Scheme);
        Assert.Equal(token, httpClient.DefaultRequestHeaders.Authorization.Parameter);
    }

    [Fact]
    public async Task GetSCIMGroup_ReturnsSCIMGroup()
    {
        // Arrange: Prepare a dummy JSON response for SCIMGroup.
        string json = @"{
                ""id"": ""dummy-id"",
                ""displayName"": ""Test Group"",
                ""externalId"": ""external-id"",
                ""urn:mace:surf.nl:sram:scim:extension:Group"": {
                    ""urn"": ""dummy-urn"",
                    ""description"": ""Dummy description"",
                    ""links"": [
                        { ""name"": ""sbs_url"", ""value"": ""http://example.com"" },
                        { ""name"": ""logo"", ""value"": ""http://example.com/logo.png"" }
                    ]
                },
                ""members"": [
                    { ""$ref"": ""ref"", ""display"": ""Test Member"", ""value"": ""member1"" }
                ]
            }";

        FakeHttpMessageHandler handler = new(json, HttpStatusCode.OK);
        HttpClient httpClient = new(handler)
        {
            BaseAddress = new("https://dummy/"),
        };
        SCIMApiClient client = new(httpClient);

        // Act
        SCIMGroup? group = await client.GetSCIMGroup("dummy-id");

        // Assert
        Assert.NotNull(group);
        Assert.Equal("dummy-id", group.Id);
        Assert.Equal("Test Group", group.DisplayName);
        Assert.NotNull(group.SCIMGroupInfo);
        Assert.Equal("dummy-urn", group.SCIMGroupInfo.Urn);
        Assert.NotEmpty(group.Members);
        Assert.Equal("Test Member", group.Members[0].Display);
    }

    /// <summary>
    /// Given a valid SCIM API client,
    /// When GetAllGroups is called,
    /// Then it should return a list of SCIMGroup objects.
    /// </summary>
    [Fact]
    public async Task GetAllGroups_ReturnsListOfSCIMGroups()
    {
        // Arrange: Prepare dummy JSON for a SCIMGroupsResult.
        // Note: The top-level property is "Resources" to match the SCIMGroupsResult class.
        const string json = """
                            {
                                "Resources": [
                                    {
                                        "id": "dummy-id",
                                        "displayName": "Test Group",
                                        "externalId": "external-id",
                                        "SCIMGroupInfo": {
                                            "urn": "dummy-urn",
                                            "description": "Dummy description",
                                            "links": [
                                                { "name": "sbs_url", "value": "http://example.com" },
                                                { "name": "logo", "value": "http://example.com/logo.png" }
                                            ]
                                        },
                                        "members": [
                                            { "$ref": "ref1", "display": "Test Member", "value": "member1", "type": "user" }
                                        ]
                                    }
                                ],
                                "itemsPerPage": 1,
                                "schemas": [ "urn:ietf:params:scim:schemas:core:2.0:ListResponse" ],
                                "startIndex": 1,
                                "totalResults": 1
                            }
                            """;

        FakeHttpMessageHandler handler = new(json, HttpStatusCode.OK);
        HttpClient httpClient = new(handler)
        {
            BaseAddress = new("https://dummy/"),
        };
        SCIMApiClient client = new(httpClient);

        // Act
        var groups = await client.GetAllGroups();

        // Assert
        Assert.NotNull(groups);
        Assert.Single(groups);
        SCIMGroup firstGroup = groups.First();
        Assert.Equal("dummy-id", firstGroup.Id);
        Assert.Equal("Test Group", firstGroup.DisplayName);
    }

    /// <summary>
    /// Given a valid SCIM API client,
    /// When GetSCIMMemberByExternalId is called with a valid ID,
    /// Then it should return a SCIMUser object.
    /// </summary>
    [Fact]
    public async Task GetSCIMMemberByExternalId_ReturnsSCIMUser()
    {
        // Arrange: Prepare dummy JSON response for a SCIMUser.
        const string json = """
                            {
                                "id": "member1",
                                "userName": "testuser",
                                "displayName": "Test User",
                                "emails": [ { "value": "testuser@example.com" } ]
                            }
                            """;

        FakeHttpMessageHandler handler = new(json, HttpStatusCode.OK);
        HttpClient httpClient = new(handler)
        {
            BaseAddress = new("https://dummy/"),
        };
        SCIMApiClient client = new(httpClient);

        // Act
        SCIMUser? user = await client.GetSCIMMemberByExternalId("member1");

        // Assert
        Assert.NotNull(user);
        Assert.Equal("member1", user.Id);
        Assert.Equal("testuser", user.UserName);
        Assert.Equal("Test User", user.DisplayName);
        Assert.NotNull(user.Emails);
        Assert.NotEmpty(user.Emails);
        Assert.Equal("testuser@example.com", user.Emails[0].Value);
    }
}
