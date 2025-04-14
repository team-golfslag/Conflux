// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
using Conflux.RepositoryConnections.SRAM;
using Conflux.RepositoryConnections.SRAM.DTOs;
using Conflux.RepositoryConnections.SRAM.Exceptions;
using Conflux.RepositoryConnections.SRAM.Models;
using Moq;
using Xunit;

namespace Conflux.Integrations.SRAM.Tests;

public class CollaborationMapperTests
{
    [Fact]
    public async Task Map_ReturnsEmptyList_WhenNoCollaborationDTOsProvided()
    {
        // Arrange
        var contextMock = new Mock<ConfluxContext>();
        var scimApiClientMock = new Mock<SCIMApiClient>();
        CollaborationMapper mapper = new(contextMock.Object, scimApiClientMock.Object);

        // Act
        var result = await mapper.Map(new());

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task Map_ThrowsException_WhenGroupNotFoundInSCIMApi()
    {
        // Arrange
        var contextMock = new Mock<ConfluxContext>();
        var scimApiClientMock = new Mock<SCIMApiClient>();
        scimApiClientMock.Setup(client => client.GetSCIMGroup(It.IsAny<string>())).ReturnsAsync((SCIMGroup?)null);

        CollaborationMapper mapper = new(contextMock.Object, scimApiClientMock.Object);
        var collaborationDTOs = new List<CollaborationDTO>
        {
            new()
            {
                Organization = "Org",
                Name = "Collab",
                Groups =
                [
                    "Group1",
                ],
            },
        };

        // Act & Assert
        await Assert.ThrowsAsync<GroupNotFoundException>(() => mapper.Map(collaborationDTOs));
    }

    [Fact]
    public async Task GetAllGroupsFromSCIMApi_ThrowsException_WhenNoGroupsFoundInSCIMApi()
    {
        // Arrange
        var contextMock = new Mock<ConfluxContext>();
        var scimApiClientMock = new Mock<SCIMApiClient>();
        scimApiClientMock.Setup(client => client.GetAllGroups()).ReturnsAsync((List<SCIMGroup>?)null);

        CollaborationMapper mapper = new(contextMock.Object, scimApiClientMock.Object);
        var collaborationDTOs = new List<CollaborationDTO>
        {
            new()
            {
                Organization = "Org",
                Name = "Collab",
                Groups = new()
                {
                    "Group1",
                },
            },
        };

        // Act & Assert
        await Assert.ThrowsAsync<GroupNotFoundException>(() => mapper.Map(collaborationDTOs));
    }

    // [Fact]
    // public async Task GetGroupFromSCIMApi_ThrowsException_WhenGroupNotFoundInDatabase()
    // {
    //     // Arrange
    //     var contextMock = new Mock<ConfluxContext>();
    //     contextMock.Setup(context => context.SRAMGroupIdConnections.FindAsync(It.IsAny<string>()))
    //         .ReturnsAsync((SRAMGroupIdConnection?)null);
    //
    //     var scimApiClientMock = new Mock<SCIMApiClient>();
    //     CollaborationMapper mapper = new(contextMock.Object, scimApiClientMock.Object);
    //
    //     // Act & Assert
    //     await Assert.ThrowsAsync<GroupNotFoundException>(() => mapper.GetType()
    //         .GetMethod("GetGroupFromSCIMApi", BindingFlags.NonPublic | BindingFlags.Instance)!
    //         .Invoke(mapper, new object[] { "urn:test" }));
    // }
    //
    // [Fact]
    // public async Task GetGroupFromSCIMApi_ThrowsException_WhenGroupNotFoundInSCIMApi()
    // {
    //     // Arrange
    //     var contextMock = new Mock<ConfluxContext>();
    //     contextMock.Setup(context => context.SRAMGroupIdConnections.FindAsync(It.IsAny<string>()))
    //         .ReturnsAsync(new SRAMGroupIdConnection
    //         {
    //             Id = "test-id",
    //         });
    //
    //     var scimApiClientMock = new Mock<SCIMApiClient>();
    //     scimApiClientMock.Setup(client => client.GetSCIMGroup(It.IsAny<string>())).ReturnsAsync((SCIMGroup?)null);
    //
    //     CollaborationMapper mapper = new(contextMock.Object, scimApiClientMock.Object);
    //
    //     // Act & Assert
    //     await Assert.ThrowsAsync<GroupNotFoundException>(() => mapper.GetType()
    //         .GetMethod("GetGroupFromSCIMApi", BindingFlags.NonPublic | BindingFlags.Instance)!
    //         .Invoke(mapper, new object[] { "urn:test" }));
    // }
}
