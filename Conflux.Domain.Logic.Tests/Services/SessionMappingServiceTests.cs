// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
//
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
using Conflux.Domain.Logic.Services;
using Conflux.Domain.Session;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Moq;
using SRAM.SCIM.Net;
using SRAM.SCIM.Net.Models;
using Xunit;

namespace Conflux.Domain.Logic.Tests.Services;

public class SessionMappingServiceTests : IDisposable
{
    private readonly ConfluxContext _context;
    private readonly Mock<IVariantFeatureManager> _mockFeatureManager;
    private readonly Mock<ISCIMApiClient> _mockScimApiClient;
    private readonly SessionMappingService _service;

    public SessionMappingServiceTests()
    {
        ServiceProvider serviceProvider = new ServiceCollection()
            .AddEntityFrameworkInMemoryDatabase()
            .BuildServiceProvider();

        DbContextOptions<ConfluxContext> options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.CreateVersion7()}")
            .UseInternalServiceProvider(serviceProvider)
            .Options;

        _context = new(options);
        _context.Database.EnsureCreated();

        _mockFeatureManager = new();
        _mockScimApiClient = new();
        _service = new(_context, _mockScimApiClient.Object, _mockFeatureManager.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    private void SetupFeatureFlag(bool isEnabled) =>
        _mockFeatureManager.Setup(m => m.IsEnabledAsync("SRAMAuthentication", CancellationToken.None))
            .ReturnsAsync(isEnabled);

    private void SetupScimApiForMember(string memberScimId, SCIMUser? userToReturn) => 
        _mockScimApiClient.Setup(c => c.GetSCIMMemberByExternalId(memberScimId)).ReturnsAsync(userToReturn);

    private static UserSession CreateUserSession(string sramId, List<Collaboration> collaborations) =>
        new()
            { Email = "test@example.com", SRAMId = sramId, Collaborations = collaborations };

    private static Collaboration CreateCollaboration(Group collaborationGroup, List<Group>? roleGroups = null) =>
        new()
        {
            CollaborationGroup = collaborationGroup,
            Groups = roleGroups ?? [],
            Organization = "Test Organization",
        };

    private static Group CreateGroup(string scimId, string displayName, string description, string urn, List<GroupMember> members) =>
        new()
        {
            SCIMId = scimId,
            DisplayName = displayName,
            Description = description,
            Urn = urn,
            Members = members,
            Id = Guid.CreateVersion7().ToString(),
            ExternalId = Guid.CreateVersion7().ToString(),
        };

    private static SCIMUser CreateScimUser(string id, string displayName, string email) =>
        new()
        {
            Id = id,
            DisplayName = displayName,
            Emails =
            [
                new()
                {
                    Value = email
                },
            ],
            Name = new()
                { GivenName = "Test", FamilyName = "User" }
        };

    [Fact]
    public async Task CollectSessionData_WhenFeatureFlagDisabled_DoesNotProcessData()
    {
        // Arrange
        SetupFeatureFlag(false);
        UserSession userSession = CreateUserSession("sram-id", []);

        // Act
        await _service.CollectSessionData(userSession);

        // Assert
        _mockScimApiClient.Verify(c => c.GetSCIMMemberByExternalId(It.IsAny<string>()), Times.Never);
        Assert.Empty(_context.Projects);
    }

    [Fact]
    public async Task CollectSessionData_WithCollaborationAndMember_AddsProjectAndUser()
    {
        // Arrange
        SetupFeatureFlag(true);
        SCIMUser scimUser = CreateScimUser("user-id-1", "Test User", "test@example.com");
        SetupScimApiForMember("member-id-1", scimUser);

        GroupMember member = new()
        {
            SCIMId = "member-id-1",
            DisplayName = "Test Member",
        };
        Group group = CreateGroup("group-id-1", "Test Group", "Test Desc", "urn:test", [member]);
        Collaboration collaboration = CreateCollaboration(group);
        UserSession userSession = CreateUserSession("sram-id-1", [collaboration]);

        // Act
        await _service.CollectSessionData(userSession);

        // Assert
        Project project = await _context.Projects.Include(p => p.Titles).SingleAsync();
        Assert.Equal("Test Group", project.Titles.Single().Text);

        User user = await _context.Users.Include(u => u.Person).SingleAsync();
        Assert.Equal("sram-id-1", user.SRAMId);
        Assert.Equal("Test User", user.Person!.Name);
    }

    [Fact]
    public async Task CollectSessionData_WithRoleGroups_AddsRolesToDatabase()
    {
        // Arrange
        SetupFeatureFlag(true);
        SCIMUser scimUser = CreateScimUser("user-id-1", "Test User", "test@example.com");
        SetupScimApiForMember("member-id-1", scimUser);

        GroupMember member = new()
        {
            SCIMId = "member-id-1",
            DisplayName = "Test Member",
        };
        Group collabGroup = CreateGroup("group-id-1", "Test Group", "Test Desc", "urn:test", [member]);
        Group roleGroup = CreateGroup("role-id-1", "Role Group", "Role Desc", "role:urn:conflux-admin", [member]);
        Collaboration collaboration = CreateCollaboration(collabGroup, [roleGroup]);
        UserSession userSession = CreateUserSession("sram-id-1", [collaboration]);

        // Act
        await _service.CollectSessionData(userSession);

        // Assert
        UserRole userRole = await _context.UserRoles.SingleAsync();
        Assert.Equal(UserRoleType.Admin, userRole.Type);
        Assert.Equal("role:urn:conflux-admin", userRole.Urn);
    }

    [Fact]
    public async Task CollectSessionData_WithExistingUser_UpdatesSRAMId()
    {
        // Arrange
        SetupFeatureFlag(true);
        SCIMUser scimUser = CreateScimUser("user-id-1", "Test User", "test@example.com");
        SetupScimApiForMember("member-id-1", scimUser);

        // Pre-populate DB with user that has same SCIM ID but no SRAM ID
        Person person = new()
            { Name = "Existing User", Email = "test@example.com" };
        _context.Users.Add(new()
        {
            SCIMId = "user-id-1",
            Person = person,
            PersonId = person.Id
        });
        await _context.SaveChangesAsync();

        GroupMember member = new()
        {
            SCIMId = "member-id-1",
            DisplayName = "Test Member",
        };
        Group group = CreateGroup("group-id-1", "Test Group", "Test Desc", "urn:test", [member]);
        Collaboration collaboration = CreateCollaboration(group);
        UserSession userSession = CreateUserSession("sram-id-1", [collaboration]);

        // Act
        await _service.CollectSessionData(userSession);

        // Assert
        User user = await _context.Users.SingleAsync();
        Assert.Equal("sram-id-1", user.SRAMId);
    }

    [Fact]
    public async Task CollectSessionData_WithExistingUserWithDifferentEmail_DoesNotUpdateSRAMId()
    {
        // Arrange
        SetupFeatureFlag(true);
        SCIMUser scimUser = CreateScimUser("user-id-1", "Test User", "different@example.com");
        SetupScimApiForMember("member-id-1", scimUser);

        // Pre-populate DB with user that has same SCIM ID but different email
        Person person = new()
            { Name = "Existing User", Email = "different@example.com" };
        _context.Users.Add(new()
        {
            SCIMId = "user-id-1",
            Person = person,
            PersonId = person.Id,
        });
        await _context.SaveChangesAsync();

        GroupMember member = new()
        {
            SCIMId = "member-id-1",
            DisplayName = "Test Member",
        };
        Group group = CreateGroup("group-id-1", "Test Group", "Test Desc", "urn:test", [member]);
        Collaboration collaboration = CreateCollaboration(group);
        // The session email does not match the user's email in the DB
        UserSession userSession = CreateUserSession("sram-id-1", [collaboration]);

        // Act
        await _service.CollectSessionData(userSession);

        // Assert
        User user = await _context.Users.SingleAsync();
        Assert.Null(user.SRAMId);
    }
}