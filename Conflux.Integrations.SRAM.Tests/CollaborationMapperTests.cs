// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
using Conflux.Domain.Session;
using Conflux.Integrations.SRAM.DTOs;
using Microsoft.EntityFrameworkCore;
using Moq;
using SRAM.SCIM.Net;
using SRAM.SCIM.Net.Exceptions;
using SRAM.SCIM.Net.Models;
using Xunit;

namespace Conflux.Integrations.SRAM.Tests;

public class CollaborationMapperTests
{
    private readonly ConfluxContext _context;
    private readonly CollaborationMapper _mapper;
    private readonly Mock<ISCIMApiClient> _mockScimApiClient;

    public CollaborationMapperTests()
    {
        var options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new(options);
        _mockScimApiClient = new();
        _mapper = new(_context, _mockScimApiClient.Object);
    }

    [Fact]
    public async Task Map_WhenAllGroupsInCache_RetrievesGroupsFromCache()
    {
        const string orgName = "org";
        const string coName = "co";
        const string groupUrn = $"urn:mace:surf.nl:sram:group:{orgName}:{coName}";

        // Add the main collaboration group and all three role groups to cache
        _context.SRAMGroupIdConnections.Add(new()
        {
            Urn = groupUrn,
            Id = "1",
        });
        _context.SRAMGroupIdConnections.Add(new()
        {
            Urn = $"urn:mace:surf.nl:sram:group:{orgName}:{coName}:conflux-admin",
            Id = "2",
        });
        _context.SRAMGroupIdConnections.Add(new()
        {
            Urn = $"urn:mace:surf.nl:sram:group:{orgName}:{coName}:conflux-contributor",
            Id = "3",
        });
        _context.SRAMGroupIdConnections.Add(new()
        {
            Urn = $"urn:mace:surf.nl:sram:group:{orgName}:{coName}:conflux-user",
            Id = "4",
        });
        await _context.SaveChangesAsync();

        List<CollaborationDTO> collaborationDTOs =
        [
            new()
            {
                Organization = orgName,
                Name = coName,
                // No need to specify groups as the mapper uses the fixed list
            },
        ];

        SCIMGroup scimGroup1 = CreateSCIMGroup("1", "Main Group", "main-urn");
        SCIMGroup scimGroup2 = CreateSCIMGroup("2", "Admin Group", "admin-urn");
        SCIMGroup scimGroup3 = CreateSCIMGroup("3", "Contributor Group", "contributor-urn");
        SCIMGroup scimGroup4 = CreateSCIMGroup("4", "User Group", "user-urn");

        _mockScimApiClient.Setup(m => m.GetSCIMGroup("1")).ReturnsAsync(scimGroup1);
        _mockScimApiClient.Setup(m => m.GetSCIMGroup("2")).ReturnsAsync(scimGroup2);
        _mockScimApiClient.Setup(m => m.GetSCIMGroup("3")).ReturnsAsync(scimGroup3);
        _mockScimApiClient.Setup(m => m.GetSCIMGroup("4")).ReturnsAsync(scimGroup4);

        var result = await _mapper.Map(collaborationDTOs);

        Assert.Single(result);
        Assert.Equal(orgName, result[0].Organization);
        Assert.Equal("Main Group", result[0].CollaborationGroup!.DisplayName);
        Assert.Equal(3, result[0].Groups!.Count);
        Assert.Equal("Admin Group", result[0].Groups![0].DisplayName);
        Assert.Equal("Contributor Group", result[0].Groups![1].DisplayName);
        Assert.Equal("User Group", result[0].Groups![2].DisplayName);

        _mockScimApiClient.Verify(m => m.GetSCIMGroup("1"), Times.Once);
        _mockScimApiClient.Verify(m => m.GetSCIMGroup("2"), Times.Once);
        _mockScimApiClient.Verify(m => m.GetSCIMGroup("3"), Times.Once);
        _mockScimApiClient.Verify(m => m.GetSCIMGroup("4"), Times.Once);
    }

    [Fact]
    public async Task Map_WhenSomeGroupsNotInCache_RetrievesAllGroupsFromApi()
    {
        const string orgName = "org";
        const string coName = "co";
        const string groupUrn = $"urn:mace:surf.nl:sram:group:{orgName}:{coName}";

        // Only add one group to cache, forcing full retrieval
        _context.SRAMGroupIdConnections.Add(new()
        {
            Urn = groupUrn,
            Id = "1",
        });
        await _context.SaveChangesAsync();

        List<CollaborationDTO> collaborationDTOs =
        [
            new()
            {
                Organization = orgName,
                Name = coName,
                // No need to specify groups as the mapper uses the fixed list
            },
        ];

        SCIMGroup scimGroup1 = CreateSCIMGroup("1", "Main Group", $"{orgName}:{coName}");
        SCIMGroup scimGroup2 = CreateSCIMGroup("2", "Admin Group", $"{orgName}:{coName}:conflux-admin");
        SCIMGroup scimGroup3 = CreateSCIMGroup("3", "Contributor Group", $"{orgName}:{coName}:conflux-contributor");
        SCIMGroup scimGroup4 = CreateSCIMGroup("4", "User Group", $"{orgName}:{coName}:conflux-user");

        List<SCIMGroup> allGroups = [scimGroup1, scimGroup2, scimGroup3, scimGroup4];
        _mockScimApiClient.Setup(m => m.GetAllGroups()).ReturnsAsync(allGroups);

        var result = await _mapper.Map(collaborationDTOs);

        Assert.Single(result);
        Assert.Equal(orgName, result[0].Organization);
        Assert.Equal("Main Group", result[0].CollaborationGroup!.DisplayName);
        Assert.Equal(3, result[0].Groups!.Count);

        // Verify cache was updated for all 4 groups
        var connections = await _context.SRAMGroupIdConnections.ToListAsync();
        Assert.Equal(4, connections.Count);
    }

    [Fact]
    public async Task Map_WithMultipleCollaborations_MapsAllCorrectly()
    {
        List<CollaborationDTO> collaborationDTOs =
        [
            new()
            {
                Organization = "org1",
                Name = "co1",
            },
            new()
            {
                Organization = "org2",
                Name = "co2",
            },
        ];

        List<SCIMGroup> scimGroups =
        [
            CreateSCIMGroup("1", "Org1 Co1", "org1:co1"),
            CreateSCIMGroup("2", "Org1 Co1 Admin", "org1:co1:conflux-admin"),
            CreateSCIMGroup("3", "Org1 Co1 Contributor", "org1:co1:conflux-contributor"),
            CreateSCIMGroup("4", "Org1 Co1 User", "org1:co1:conflux-user"),
            CreateSCIMGroup("5", "Org2 Co2", "org2:co2"),
            CreateSCIMGroup("6", "Org2 Co2 Admin", "org2:co2:conflux-admin"),
            CreateSCIMGroup("7", "Org2 Co2 Contributor", "org2:co2:conflux-contributor"),
            CreateSCIMGroup("8", "Org2 Co2 User", "org2:co2:conflux-user"),
        ];
        _mockScimApiClient.Setup(m => m.GetAllGroups()).ReturnsAsync(scimGroups);

        var result = await _mapper.Map(collaborationDTOs);

        Assert.Equal(2, result.Count);

        Assert.Equal("org1", result[0].Organization);
        Assert.Equal("Org1 Co1", result[0].CollaborationGroup!.DisplayName);
        Assert.Equal(3, result[0].Groups!.Count);

        Assert.Equal("org2", result[1].Organization);
        Assert.Equal("Org2 Co2", result[1].CollaborationGroup!.DisplayName);
        Assert.Equal(3, result[1].Groups!.Count);
    }

    [Fact]
    public void FormatGroupUrn_WithGroupName_FormatsCorrectly()
    {
        const string orgName = "org";
        const string coName = "co";
        const string groupName = "group";

        // Use instance method instead of static method
        string result = CollaborationMapper.FormatGroupUrn(orgName, coName, groupName);

        Assert.Equal("urn:mace:surf.nl:sram:group:org:co:group", result);
    }

    [Fact]
    public void MapSCIMGroup_MapsAllProperties()
    {
        const string groupUrn = "urn:mace:surf.nl:sram:group:org:co";
        SCIMGroup scimGroup = new()
        {
            Id = "123",
            ExternalId = "ext-123",
            DisplayName = "Test Group",
            Members =
            [
                new()
                {
                    Display = "User 1",
                    Value = "user1",
                    Ref = "",
                },

                new()
                {
                    Display = "User 2",
                    Value = "user2",
                    Ref = "",
                },
            ],
            SCIMGroupInfo = new()
            {
                Urn = "org:co",
                Description = "Test description",
                Links =
                [
                    new()
                    {
                        Name = "sbs_url",
                        Value = "https://example.com",
                    },

                    new()
                    {
                        Name = "logo",
                        Value = "https://example.com/logo.png",
                    },
                ],
            },
            SCIMMeta = new()
            {
                Created = new(2023,
                    1,
                    1,
                    0,
                    0,
                    0,
                    DateTimeKind.Utc),
                Location = "https://example.com",
                ResourceType = "Group",
                Version = "1.0",
            },
            Schemas =
            [
                "urn:mace:surf.nl:sram:scim:extension:Group",
            ],
        };

        // Use instance method instead of static method
        Group result = CollaborationMapper.MapSCIMGroup(groupUrn, scimGroup);

        Assert.Equal("org:co", result.Id);
        Assert.Equal(groupUrn, result.Urn);
        Assert.Equal("Test Group", result.DisplayName);
        Assert.Equal("Test description", result.Description);
        Assert.Equal("https://example.com", result.Url);
        Assert.Equal("https://example.com/logo.png", result.LogoUrl);
        Assert.Equal("ext-123", result.ExternalId);
        Assert.Equal("123", result.SCIMId);
        Assert.Equal(2, result.Members.Count);
        Assert.Equal(new(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc), result.Created);
    }

    private static SCIMGroup CreateSCIMGroup(string id, string displayName, string urn) =>
        new()
        {
            Id = id,
            DisplayName = displayName,
            Members = [],
            SCIMGroupInfo = new()
            {
                Urn = urn,
                Links = [],
            },
            SCIMMeta = new()
            {
                Location = "https://example.com",
                ResourceType = "Group",
                Version = "1.0",
            },
            ExternalId = "ext-123",
            Schemas = [],
        };

    [Fact]
    public async Task GetAllGroupsFromSCIMApi_WhenNoGroupsFound_ThrowsGroupNotFoundException()
    {
        // Set up
        List<CollaborationDTO> collaborationDTOs =
        [
            new()
            {
                Organization = "org",
                Name = "co",
                Groups = ["group"],
            },
        ];

        _mockScimApiClient.Setup(m => m.GetAllGroups()).ReturnsAsync((List<SCIMGroup>)null!);

        // Test exception
        await Assert.ThrowsAsync<GroupNotFoundException>(() =>
            _mapper.GetAllGroupsFromSCIMApi(collaborationDTOs));
    }

    [Fact]
    public async Task GetGroupFromSCIMApi_WhenGroupNotInDatabase_ThrowsGroupNotFoundException()
    {
        // Set up - don't add anything to the database
        const string groupUrn = "urn:mace:surf.nl:sram:group:org:co";

        // Test exception
        GroupNotFoundException exception = await Assert.ThrowsAsync<GroupNotFoundException>(() =>
            _mapper.GetGroupFromSCIMApi(groupUrn));

        Assert.Contains(groupUrn, exception.Message);
    }

    [Fact]
    public async Task GetGroupFromSCIMApi_WhenGroupNotFoundInApi_ThrowsGroupNotFoundException()
    {
        // Set up
        const string groupUrn = "urn:mace:surf.nl:sram:group:org:co";
        const string groupId = "123";

        _context.SRAMGroupIdConnections.Add(new()
        {
            Urn = groupUrn,
            Id = groupId,
        });
        await _context.SaveChangesAsync();

        _mockScimApiClient.Setup(m => m.GetSCIMGroup(groupId)).ReturnsAsync((SCIMGroup)null!);

        // Test exception
        GroupNotFoundException exception = await Assert.ThrowsAsync<GroupNotFoundException>(() =>
            _mapper.GetGroupFromSCIMApi(groupUrn));

        Assert.Contains(groupId, exception.Message);
    }

    [Fact]
    public async Task Map_WhenNoGroupsFoundInApi_ThrowsGroupNotFoundException()
    {
        // Set up
        List<CollaborationDTO> collaborationDTOs =
        [
            new()
            {
                Organization = "org",
                Name = "co",
                Groups = ["group"],
            },
        ];

        // This will force fetching all groups since not all URNs are in cache
        _mockScimApiClient.Setup(m => m.GetAllGroups()).ReturnsAsync((List<SCIMGroup>)null!);

        // Test that Map passes through the exception from GetAllGroupsFromSCIMApi
        await Assert.ThrowsAsync<GroupNotFoundException>(() => _mapper.Map(collaborationDTOs));
    }
}
