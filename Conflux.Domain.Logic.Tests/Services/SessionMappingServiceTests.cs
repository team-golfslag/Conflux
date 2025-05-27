// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
using Conflux.Domain.Logic.Services;
using Conflux.Domain.Session;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using Moq;
using SRAM.SCIM.Net;
using SRAM.SCIM.Net.Models;
using Xunit;

namespace Conflux.Domain.Logic.Tests.Services;

public class SessionMappingServiceTests
{
    [Fact]
    public async Task CollectSessionData_WhenFeatureFlagDisabled_DoesNotProcessData()
    {
        // Arrange
        var mockFeatureManager = new Mock<IVariantFeatureManager>();
        mockFeatureManager.Setup(m => m.IsEnabledAsync("SRAMAuthentication", CancellationToken.None))
            .ReturnsAsync(false);

        var mockSCIMApiClient = new Mock<ISCIMApiClient>();

        var options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase("TestDb_" + Guid.NewGuid())
            .Options;

        ConfluxContext context = new(options);

        SessionMappingService service = new(context, mockSCIMApiClient.Object, mockFeatureManager.Object);

        UserSession userSession = new()
        {
            Email = "test@example.com",
            SRAMId = "sram-id-1",
            Collaborations = new(),
        };

        // Act
        await service.CollectSessionData(userSession);

        // Assert
        mockSCIMApiClient.Verify(c => c.GetSCIMMemberByExternalId(It.IsAny<string>()), Times.Never);
        Assert.Empty(context.Projects);
        Assert.Empty(context.Users);
        Assert.Empty(context.UserRoles);
    }

    [Fact]
    public async Task CollectSessionData_WithNoCollaborations_DoesNotAddAnyData()
    {
        // Arrange
        var mockFeatureManager = new Mock<IVariantFeatureManager>();
        mockFeatureManager.Setup(m => m.IsEnabledAsync("SRAMAuthentication", CancellationToken.None))
            .ReturnsAsync(true);

        var mockSCIMApiClient = new Mock<ISCIMApiClient>();

        var options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase("TestDb_" + Guid.NewGuid())
            .Options;

        ConfluxContext context = new(options);

        SessionMappingService service = new(context, mockSCIMApiClient.Object, mockFeatureManager.Object);

        UserSession userSession = new()
        {
            Email = "test@example.com",
            SRAMId = "sram-id-1",
            Collaborations = new(),
        };

        // Act
        await service.CollectSessionData(userSession);

        // Assert
        Assert.Empty(context.Projects);
        Assert.Empty(context.Users);
        Assert.Empty(context.UserRoles);
    }

    [Fact]
    public async Task CollectSessionData_WithCollaborationButNoMembers_AddsProjectButNoUsers()
    {
        // Arrange
        var mockFeatureManager = new Mock<IVariantFeatureManager>();
        mockFeatureManager.Setup(m => m.IsEnabledAsync("SRAMAuthentication", CancellationToken.None))
            .ReturnsAsync(true);

        var mockSCIMApiClient = new Mock<ISCIMApiClient>();

        var options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase("TestDb_" + Guid.NewGuid())
            .Options;

        ConfluxContext context = new(options);

        SessionMappingService service = new(context, mockSCIMApiClient.Object, mockFeatureManager.Object);

        Group collaborationGroup = new()
        {
            Id = Guid.NewGuid().ToString(),
            SCIMId = "group-id-1",
            DisplayName = "Test Group",
            Description = "Test Description",
            Created = DateTime.UtcNow,
            Urn = "urn:test:group",
            ExternalId = "ext-id-1",
            Members = [],
        };

        UserSession userSession = new()
        {
            Email = "test@example.com",
            SRAMId = "sram-id-1",
            Collaborations =
            [
                new()
                {
                    Organization = "test-org",
                    CollaborationGroup = collaborationGroup,
                    Groups = [],
                },
            ],
        };

        // Act
        await service.CollectSessionData(userSession);

        // Assert
        Assert.Single(context.Projects);
        Project project = await context.Projects
            .Include(project => project.Titles)
            .Include(project => project.Descriptions)
            .FirstAsync();
        Assert.Single(project.Titles);
        Assert.Equal("Test Group", project.Titles[0].Text);
        Assert.Single(project.Descriptions);
        Assert.Equal("Test Description", project.Descriptions[0].Text);
        Assert.Empty(context.Users);
        Assert.Empty(context.UserRoles);
    }

    [Fact]
    public async Task CollectSessionData_WithCollaborationAndMember_AddsProjectAndUser()
    {
        // Arrange
        var mockFeatureManager = new Mock<IVariantFeatureManager>();
        mockFeatureManager.Setup(m => m.IsEnabledAsync("SRAMAuthentication", CancellationToken.None))
            .ReturnsAsync(true);

        var mockSCIMApiClient = new Mock<ISCIMApiClient>();
        SCIMUser scimUser = new()
        {
            Id = "user-id-1",
            DisplayName = "Test User",
            UserName = "testuser",
            Name = new()
            {
                GivenName = "Test",
                FamilyName = "User",
            },
            Emails = new()
            {
                new()
                {
                    Value = "test@example.com",
                },
            },
        };
        mockSCIMApiClient.Setup(c => c.GetSCIMMemberByExternalId("member-id-1")).ReturnsAsync(scimUser);

        var options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase("TestDb_" + Guid.NewGuid())
            .Options;

        ConfluxContext context = new(options);

        SessionMappingService service = new(context, mockSCIMApiClient.Object, mockFeatureManager.Object);

        GroupMember member = new()
        {
            SCIMId = "member-id-1",
            DisplayName = "Test Member",
        };

        Group collaborationGroup = new()
        {
            Id = Guid.NewGuid().ToString(),
            SCIMId = "group-id-1",
            DisplayName = "Test Group",
            Description = "Test Description",
            Created = DateTime.UtcNow,
            Urn = "urn:test:group",
            ExternalId = "ext-id-1",
            Members = new()
            {
                member,
            },
        };

        UserSession userSession = new()
        {
            Email = "test@example.com",
            SRAMId = "sram-id-1",
            Collaborations = new()
            {
                new()
                {
                    Organization = "test-org",
                    CollaborationGroup = collaborationGroup,
                    Groups = new(),
                },
            },
        };

        // Act
        await service.CollectSessionData(userSession);

        // Assert
        Assert.Single(context.Projects);
        Project project = await context.Projects.Include(project => project.Titles).FirstAsync();
        Assert.Single(project.Titles);
        Assert.Equal("Test Group", project.Titles[0].Text);

        Assert.Single(context.Users);
        User user = await context.Users.FirstAsync();
        Assert.Equal("Test User", user.Name);
        Assert.Equal("Test", user.GivenName);
        Assert.Equal("User", user.FamilyName);
        Assert.Equal("test@example.com", user.Email);
        Assert.Equal("sram-id-1", user.SRAMId);

        Assert.Empty(context.UserRoles);
    }

    [Fact]
    public async Task CollectSessionData_WithRoleGroups_AddsRolesToDatabase()
    {
        // Arrange
        var mockFeatureManager = new Mock<IVariantFeatureManager>();
        mockFeatureManager.Setup(m => m.IsEnabledAsync("SRAMAuthentication", CancellationToken.None))
            .ReturnsAsync(true);

        var mockSCIMApiClient = new Mock<ISCIMApiClient>();
        SCIMUser scimUser = new()
        {
            Id = "user-id-1",
            DisplayName = "Test User",
            UserName = "testuser",
            Name = new()
            {
                GivenName = "Test",
                FamilyName = "User",
            },
            Emails = new()
            {
                new()
                {
                    Value = "test@example.com",
                },
            },
        };
        mockSCIMApiClient.Setup(c => c.GetSCIMMemberByExternalId("member-id-1")).ReturnsAsync(scimUser);

        var options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase("TestDb_" + Guid.NewGuid())
            .Options;

        ConfluxContext context = new(options);

        SessionMappingService service = new(context, mockSCIMApiClient.Object, mockFeatureManager.Object);

        GroupMember member = new()
        {
            SCIMId = "member-id-1",
            DisplayName = "Test Member",
        };

        Group roleGroup = new()
        {
            Id = Guid.NewGuid().ToString(),
            SCIMId = "role-id-1",
            DisplayName = "Role Group",
            Description = "Role Description",
            Urn = "role:urn:conflux-admin",
            ExternalId = "ext-role-1",
            Created = DateTime.UtcNow,
            Members = new()
            {
                member,
            },
        };

        Group collaborationGroup = new()
        {
            Id = Guid.NewGuid().ToString(),
            SCIMId = "group-id-1",
            DisplayName = "Test Group",
            Description = "Test Description",
            Created = DateTime.UtcNow,
            Urn = "urn:test:conflux-admin",
            ExternalId = "ext-id-1",
            Members = new()
            {
                member,
            },
        };

        UserSession userSession = new()
        {
            Email = "test@example.com",
            SRAMId = "sram-id-1",
            Collaborations = new()
            {
                new()
                {
                    Organization = "test-org",
                    CollaborationGroup = collaborationGroup,
                    Groups = new()
                    {
                        roleGroup,
                    },
                },
            },
        };

        // Act
        await service.CollectSessionData(userSession);

        // Assert
        Assert.Single(context.Projects);
        Assert.Single(context.Users);
        Assert.Single(context.UserRoles);

        UserRole userRole = await context.UserRoles.FirstAsync();
        Assert.Equal(UserRoleType.Admin, userRole.Type);
        Assert.Equal("role:urn:conflux-admin", userRole.Urn);
    }

    [Fact]
    public async Task CollectSessionData_WithExistingProject_UpdatesProjectInfo()
    {
        // Arrange
        var mockFeatureManager = new Mock<IVariantFeatureManager>();
        mockFeatureManager.Setup(m => m.IsEnabledAsync("SRAMAuthentication", CancellationToken.None))
            .ReturnsAsync(true);

        var mockSCIMApiClient = new Mock<ISCIMApiClient>();

        var options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase("TestDb_" + Guid.NewGuid())
            .Options;

        ConfluxContext context = new(options);

        DateTime startDate = DateTime.UtcNow.AddDays(-10);
        Guid projectId = Guid.NewGuid();
        // Add existing project
        context.Projects.Add(new()
        {
            Id = projectId,
            SCIMId = "group-id-1",
            Titles =
            [
                new()
                {
                    ProjectId = projectId,
                    Text = "Old Title",
                    Type = TitleType.Primary,
                    StartDate = startDate,
                    EndDate = null,
                },
            ],
            Descriptions =
            [
                new()
                {
                    ProjectId = projectId,
                    Text = "Old Description",
                    Type = DescriptionType.Primary,
                    Language = Language.ENGLISH,
                },
            ],
            StartDate = startDate,
        });
        await context.SaveChangesAsync();

        SessionMappingService service = new(context, mockSCIMApiClient.Object, mockFeatureManager.Object);

        Group collaborationGroup = new()
        {
            Id = Guid.NewGuid().ToString(),
            SCIMId = "group-id-1",
            DisplayName = "New Title",
            Description = "New Description",
            Created = DateTime.UtcNow,
            Urn = "urn:test:group",
            ExternalId = "ext-id-1",
            Members = new(),
        };

        UserSession userSession = new()
        {
            Email = "test@example.com",
            SRAMId = "sram-id-1",
            Collaborations = new()
            {
                new()
                {
                    Organization = "test-org",
                    CollaborationGroup = collaborationGroup,
                    Groups = new(),
                },
            },
        };

        // Act
        await service.CollectSessionData(userSession);

        // Assert
        Assert.Single(context.Projects);
        Project project = await context.Projects
            .Include(project => project.Titles)
            .Include(project => project.Descriptions)
            .FirstAsync();
        Assert.Single(project.Titles);
        Assert.Equal("New Title", project.Titles[0].Text);
        Assert.Single(project.Descriptions);
        Assert.Equal("New Description", project.Descriptions[0].Text);
    }

    [Fact]
    public async Task CollectSessionData_WhenUserSessionIsNull_DoesNotProcessData()
    {
        // Arrange
        var mockFeatureManager = new Mock<IVariantFeatureManager>();
        mockFeatureManager.Setup(m => m.IsEnabledAsync("SRAMAuthentication", CancellationToken.None))
            .ReturnsAsync(true);

        var mockSCIMApiClient = new Mock<ISCIMApiClient>();

        var options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase("TestDb_" + Guid.NewGuid())
            .Options;

        ConfluxContext context = new(options);

        SessionMappingService service = new(context, mockSCIMApiClient.Object, mockFeatureManager.Object);

        UserSession? userSession = null;

        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(() => service.CollectSessionData(userSession!));
    }

    [Fact]
    public async Task CollectSessionData_WithCollaborationAndMissingUser_DoesNotAddUser()
    {
        // Arrange
        var mockFeatureManager = new Mock<IVariantFeatureManager>();
        mockFeatureManager.Setup(m => m.IsEnabledAsync("SRAMAuthentication", CancellationToken.None))
            .ReturnsAsync(true);

        var mockSCIMApiClient = new Mock<ISCIMApiClient>();
        mockSCIMApiClient.Setup(c => c.GetSCIMMemberByExternalId("missing-user-id")).ReturnsAsync((SCIMUser?)null);

        var options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase("TestDb_" + Guid.NewGuid())
            .Options;

        ConfluxContext context = new(options);

        SessionMappingService service = new(context, mockSCIMApiClient.Object, mockFeatureManager.Object);

        GroupMember member = new()
        {
            SCIMId = "missing-user-id",
            DisplayName = "Missing User",
        };

        Group collaborationGroup = new()
        {
            Id = Guid.NewGuid().ToString(),
            SCIMId = "group-id-1",
            DisplayName = "Test Group",
            Description = "Test Description",
            Created = DateTime.UtcNow,
            Urn = "urn:test:group",
            ExternalId = "ext-id-1",
            Members = new()
            {
                member,
            },
        };

        UserSession userSession = new()
        {
            Email = "test@example.com",
            SRAMId = "sram-id-1",
            Collaborations = new()
            {
                new()
                {
                    Organization = "test-org",
                    CollaborationGroup = collaborationGroup,
                    Groups = new(),
                },
            },
        };

        // Act
        await service.CollectSessionData(userSession);

        // Assert
        Assert.Single(context.Projects);
        Assert.Empty(context.Users);
    }

    [Fact]
    public async Task CollectSessionData_WithExistingUser_UpdatesSRAMId()
    {
        // Arrange
        var mockFeatureManager = new Mock<IVariantFeatureManager>();
        mockFeatureManager.Setup(m => m.IsEnabledAsync("SRAMAuthentication", CancellationToken.None))
            .ReturnsAsync(true);

        var mockSCIMApiClient = new Mock<ISCIMApiClient>();
        SCIMUser scimUser = new()
        {
            Id = "user-id-1",
            DisplayName = "Test User",
            UserName = "testuser",
            Name = new()
            {
                GivenName = "Test",
                FamilyName = "User",
            },
            Emails = new()
            {
                new()
                {
                    Value = "test@example.com",
                },
            },
        };
        mockSCIMApiClient.Setup(c => c.GetSCIMMemberByExternalId("member-id-1")).ReturnsAsync(scimUser);

        var options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase("TestDb_" + Guid.NewGuid())
            .Options;

        ConfluxContext context = new(options);

        // Add existing user without SRAM ID
        context.Users.Add(new()
        {
            SCIMId = "user-id-1",
            Name = "Existing User",
            Email = "test@example.com",
        });
        await context.SaveChangesAsync();

        SessionMappingService service = new(context, mockSCIMApiClient.Object, mockFeatureManager.Object);

        GroupMember member = new()
        {
            SCIMId = "member-id-1",
            DisplayName = "Test Member",
        };

        Group collaborationGroup = new()
        {
            Id = Guid.NewGuid().ToString(),
            SCIMId = "group-id-1",
            DisplayName = "Test Group",
            Description = "Test Description",
            Created = DateTime.UtcNow,
            Urn = "urn:test:group",
            ExternalId = "ext-id-1",
            Members = new()
            {
                member,
            },
        };

        UserSession userSession = new()
        {
            Email = "test@example.com",
            SRAMId = "sram-id-1",
            Collaborations = new()
            {
                new()
                {
                    Organization = "test-org",
                    CollaborationGroup = collaborationGroup,
                    Groups = new(),
                },
            },
        };

        // Act
        await service.CollectSessionData(userSession);

        // Assert
        Assert.Single(context.Users);
        User user = await context.Users.FirstAsync();
        Assert.Equal("sram-id-1", user.SRAMId);
    }

    [Fact]
    public async Task CollectSessionData_WithExistingUserWithDifferentEmail_DoesNotUpdateSRAMId()
    {
        // Arrange
        var mockFeatureManager = new Mock<IVariantFeatureManager>();
        mockFeatureManager.Setup(m => m.IsEnabledAsync("SRAMAuthentication", CancellationToken.None))
            .ReturnsAsync(true);

        var mockSCIMApiClient = new Mock<ISCIMApiClient>();
        SCIMUser scimUser = new()
        {
            Id = "user-id-1",
            DisplayName = "Test User",
            UserName = "testuser",
            Name = new()
            {
                GivenName = "Test",
                FamilyName = "User",
            },
            Emails = new()
            {
                new()
                {
                    Value = "different@example.com",
                },
            },
        };
        mockSCIMApiClient.Setup(c => c.GetSCIMMemberByExternalId("member-id-1")).ReturnsAsync(scimUser);

        var options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase("TestDb_" + Guid.NewGuid())
            .Options;

        ConfluxContext context = new(options);

        // Add existing user with different email
        context.Users.Add(new()
        {
            SCIMId = "user-id-1",
            Name = "Existing User",
            Email = "different@example.com",
        });
        await context.SaveChangesAsync();

        SessionMappingService service = new(context, mockSCIMApiClient.Object, mockFeatureManager.Object);

        GroupMember member = new()
        {
            SCIMId = "member-id-1",
            DisplayName = "Test Member",
        };

        Group collaborationGroup = new()
        {
            Id = Guid.NewGuid().ToString(),
            SCIMId = "group-id-1",
            DisplayName = "Test Group",
            Description = "Test Description",
            Created = DateTime.UtcNow,
            Urn = "urn:test:group",
            ExternalId = "ext-id-1",
            Members = new()
            {
                member,
            },
        };

        UserSession userSession = new()
        {
            Email = "test@example.com",
            SRAMId = "sram-id-1",
            Collaborations = new()
            {
                new()
                {
                    Organization = "test-org",
                    CollaborationGroup = collaborationGroup,
                    Groups = new(),
                },
            },
        };

        // Act
        await service.CollectSessionData(userSession);

        // Assert
        Assert.Single(context.Users);
        User user = await context.Users.FirstAsync();
        Assert.Null(user.SRAMId);
    }
}
