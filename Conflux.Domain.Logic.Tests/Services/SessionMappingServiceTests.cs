using Conflux.Data;
using Conflux.Domain.Logic.Services;
using Conflux.Domain.Models;
using Conflux.RepositoryConnections.SRAM;
using Conflux.RepositoryConnections.SRAM.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Conflux.Domain.Logic.Tests.Services;

public class SessionMappingServiceTests
{
    [Fact]
    public async Task CollectSessionData_WhenFeatureFlagDisabled_DoesNotProcessData()
    {
        // Arrange
        var mockFeatureManager = new Mock<IVariantFeatureManager>();
        mockFeatureManager.Setup(m => m.IsEnabledAsync("SRAMAuthentication", CancellationToken.None)).ReturnsAsync(false);

        var mockSCIMApiClient = new Mock<ISCIMApiClient>();

        var options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_" + Guid.NewGuid())
            .Options;

        var context = new ConfluxContext(options);

        var service = new SessionMappingService(context, mockSCIMApiClient.Object, mockFeatureManager.Object);

        var userSession = new UserSession
        {
            Email = "test@example.com",
            SRAMId = "sram-id-1",
            Collaborations = new List<Collaboration>()
        };

        // Act
        await service.CollectSessionData(userSession);

        // Assert
        mockSCIMApiClient.Verify(c => c.GetSCIMMemberByExternalId(It.IsAny<string>()), Times.Never);
        Assert.Empty(context.Projects);
        Assert.Empty(context.Users);
        Assert.Empty(context.Roles);
    }

    [Fact]
    public async Task CollectSessionData_WithNoCollaborations_DoesNotAddAnyData()
    {
        // Arrange
        var mockFeatureManager = new Mock<IVariantFeatureManager>();
        mockFeatureManager.Setup(m => m.IsEnabledAsync("SRAMAuthentication", CancellationToken.None)).ReturnsAsync(true);

        var mockSCIMApiClient = new Mock<ISCIMApiClient>();

        var options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_" + Guid.NewGuid())
            .Options;

        var context = new ConfluxContext(options);

        var service = new SessionMappingService(context, mockSCIMApiClient.Object, mockFeatureManager.Object);

        var userSession = new UserSession
        {
            Email = "test@example.com",
            SRAMId = "sram-id-1",
            Collaborations = new List<Collaboration>()
        };

        // Act
        await service.CollectSessionData(userSession);

        // Assert
        Assert.Empty(context.Projects);
        Assert.Empty(context.Users);
        Assert.Empty(context.Roles);
    }

    [Fact]
    public async Task CollectSessionData_WithCollaborationButNoMembers_AddsProjectButNoUsers()
    {
        // Arrange
        var mockFeatureManager = new Mock<IVariantFeatureManager>();
        mockFeatureManager.Setup(m => m.IsEnabledAsync("SRAMAuthentication", CancellationToken.None)).ReturnsAsync(true);

        var mockSCIMApiClient = new Mock<ISCIMApiClient>();

        var options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_" + Guid.NewGuid())
            .Options;

        var context = new ConfluxContext(options);

        var service = new SessionMappingService(context, mockSCIMApiClient.Object, mockFeatureManager.Object);

        var collaborationGroup = new Group
        {
            Id = Guid.NewGuid().ToString(),
            SCIMId = "group-id-1",
            DisplayName = "Test Group",
            Description = "Test Description",
            Created = DateTime.UtcNow,
            Urn = "urn:test:group",
            ExternalId = "ext-id-1",
            Members = new List<GroupMember>()
        };

        var userSession = new UserSession
        {
            Email = "test@example.com",
            SRAMId = "sram-id-1",
            Collaborations = new List<Collaboration>
            {
                new Collaboration
                {
                    Organization = "test-org",
                    CollaborationGroup = collaborationGroup,
                    Groups = new List<Group>()
                }
            }
        };

        // Act
        await service.CollectSessionData(userSession);

        // Assert
        Assert.Single(context.Projects);
        Assert.Equal("Test Group", context.Projects.First().Title);
        Assert.Equal("Test Description", context.Projects.First().Description);
        Assert.Empty(context.Users);
        Assert.Empty(context.Roles);
    }

    [Fact]
    public async Task CollectSessionData_WithCollaborationAndMember_AddsProjectAndUser()
    {
        // Arrange
        var mockFeatureManager = new Mock<IVariantFeatureManager>();
        mockFeatureManager.Setup(m => m.IsEnabledAsync("SRAMAuthentication", CancellationToken.None)).ReturnsAsync(true);

        var mockSCIMApiClient = new Mock<ISCIMApiClient>();
        var scimUser = new SCIMUser
        {
            Id = "user-id-1",
            DisplayName = "Test User",
            UserName = "testuser",
            Name = new SCIMName
            {
                GivenName = "Test",
                FamilyName = "User"
            },
            Emails = new List<SCIMEmail>
            {
                new SCIMEmail { Value = "test@example.com" }
            }
        };
        mockSCIMApiClient.Setup(c => c.GetSCIMMemberByExternalId("member-id-1")).ReturnsAsync(scimUser);

        var options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_" + Guid.NewGuid())
            .Options;

        var context = new ConfluxContext(options);

        var service = new SessionMappingService(context, mockSCIMApiClient.Object, mockFeatureManager.Object);

        var member = new GroupMember
        {
            SCIMId = "member-id-1",
            DisplayName = "Test Member"
        };

        var collaborationGroup = new Group
        {
            Id = Guid.NewGuid().ToString(),
            SCIMId = "group-id-1",
            DisplayName = "Test Group",
            Description = "Test Description",
            Created = DateTime.UtcNow,
            Urn = "urn:test:group",
            ExternalId = "ext-id-1",
            Members = new List<GroupMember> { member }
        };

        var userSession = new UserSession
        {
            Email = "test@example.com",
            SRAMId = "sram-id-1",
            Collaborations = new List<Collaboration>
            {
                new Collaboration
                {
                    Organization = "test-org",
                    CollaborationGroup = collaborationGroup,
                    Groups = new List<Group>()
                }
            }
        };

        // Act
        await service.CollectSessionData(userSession);

        // Assert
        Assert.Single(context.Projects);
        Assert.Equal("Test Group", context.Projects.First().Title);

        Assert.Single(context.Users);
        var user = context.Users.First();
        Assert.Equal("Test User", user.Name);
        Assert.Equal("Test", user.GivenName);
        Assert.Equal("User", user.FamilyName);
        Assert.Equal("test@example.com", user.Email);
        Assert.Equal("sram-id-1", user.SRAMId);

        Assert.Empty(context.Roles);
    }

    [Fact]
    public async Task CollectSessionData_WithRoleGroups_AddsRolesToDatabase()
    {
        // Arrange
        var mockFeatureManager = new Mock<IVariantFeatureManager>();
        mockFeatureManager.Setup(m => m.IsEnabledAsync("SRAMAuthentication", CancellationToken.None)).ReturnsAsync(true);

        var mockSCIMApiClient = new Mock<ISCIMApiClient>();
        var scimUser = new SCIMUser
        {
            Id = "user-id-1",
            DisplayName = "Test User",
            UserName = "testuser",
            Name = new SCIMName
            {
                GivenName = "Test",
                FamilyName = "User"
            },
            Emails = new List<SCIMEmail>
            {
                new SCIMEmail { Value = "test@example.com" }
            }
        };
        mockSCIMApiClient.Setup(c => c.GetSCIMMemberByExternalId("member-id-1")).ReturnsAsync(scimUser);

        var options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_" + Guid.NewGuid())
            .Options;

        var context = new ConfluxContext(options);

        var service = new SessionMappingService(context, mockSCIMApiClient.Object, mockFeatureManager.Object);

        var member = new GroupMember
        {
            SCIMId = "member-id-1",
            DisplayName = "Test Member"
        };

        var roleGroup = new Group
        {
            Id = Guid.NewGuid().ToString(),
            SCIMId = "role-id-1",
            DisplayName = "Role Group",
            Description = "Role Description",
            Urn = "role:urn:1",
            ExternalId = "ext-role-1",
            Created = DateTime.UtcNow,
            Members = new List<GroupMember> { member }
        };

        var collaborationGroup = new Group
        {
            Id = Guid.NewGuid().ToString(),
            SCIMId = "group-id-1",
            DisplayName = "Test Group",
            Description = "Test Description",
            Created = DateTime.UtcNow,
            Urn = "urn:test:group",
            ExternalId = "ext-id-1",
            Members = new List<GroupMember> { member }
        };

        var userSession = new UserSession
        {
            Email = "test@example.com",
            SRAMId = "sram-id-1",
            Collaborations = new List<Collaboration>
            {
                new Collaboration
                {
                    Organization = "test-org",
                    CollaborationGroup = collaborationGroup,
                    Groups = new List<Group> { roleGroup }
                }
            }
        };

        // Act
        await service.CollectSessionData(userSession);

        // Assert
        Assert.Single(context.Projects);
        Assert.Single(context.Users);
        Assert.Single(context.Roles);

        var role = context.Roles.First();
        Assert.Equal("Role Group", role.Name);
        Assert.Equal("Role Description", role.Description);
        Assert.Equal("role:urn:1", role.Urn);
    }

    [Fact]
    public async Task CollectSessionData_WithExistingProject_UpdatesProjectInfo()
    {
        // Arrange
        var mockFeatureManager = new Mock<IVariantFeatureManager>();
        mockFeatureManager.Setup(m => m.IsEnabledAsync("SRAMAuthentication", CancellationToken.None)).ReturnsAsync(true);

        var mockSCIMApiClient = new Mock<ISCIMApiClient>();

        var options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_" + Guid.NewGuid())
            .Options;

        var context = new ConfluxContext(options);

        // Add existing project
        context.Projects.Add(new Project
        {
            SCIMId = "group-id-1",
            Title = "Old Title",
            Description = "Old Description",
            StartDate = DateTime.UtcNow.AddDays(-10)
        });
        await context.SaveChangesAsync();

        var service = new SessionMappingService(context, mockSCIMApiClient.Object, mockFeatureManager.Object);

        var collaborationGroup = new Group
        {
            Id = Guid.NewGuid().ToString(),
            SCIMId = "group-id-1",
            DisplayName = "New Title",
            Description = "New Description",
            Created = DateTime.UtcNow,
            Urn = "urn:test:group",
            ExternalId = "ext-id-1",
            Members = new List<GroupMember>()
        };

        var userSession = new UserSession
        {
            Email = "test@example.com",
            SRAMId = "sram-id-1",
            Collaborations = new List<Collaboration>
            {
                new Collaboration
                {
                    Organization = "test-org",
                    CollaborationGroup = collaborationGroup,
                    Groups = new List<Group>()
                }
            }
        };

        // Act
        await service.CollectSessionData(userSession);

        // Assert
        Assert.Single(context.Projects);
        var project = context.Projects.First();
        Assert.Equal("New Title", project.Title);
        Assert.Equal("New Description", project.Description);
    }

    [Fact]
    public async Task CollectSessionData_WhenUserSessionIsNull_DoesNotProcessData()
    {
        // Arrange
        var mockFeatureManager = new Mock<IVariantFeatureManager>();
        mockFeatureManager.Setup(m => m.IsEnabledAsync("SRAMAuthentication", CancellationToken.None)).ReturnsAsync(true);

        var mockSCIMApiClient = new Mock<ISCIMApiClient>();

        var options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_" + Guid.NewGuid())
            .Options;

        var context = new ConfluxContext(options);

        var service = new SessionMappingService(context, mockSCIMApiClient.Object, mockFeatureManager.Object);

        UserSession? userSession = null;

        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(() => service.CollectSessionData(userSession!));
    }

    [Fact]
    public async Task CollectSessionData_WithCollaborationAndMissingUser_DoesNotAddUser()
    {
        // Arrange
        var mockFeatureManager = new Mock<IVariantFeatureManager>();
        mockFeatureManager.Setup(m => m.IsEnabledAsync("SRAMAuthentication", CancellationToken.None)).ReturnsAsync(true);

        var mockSCIMApiClient = new Mock<ISCIMApiClient>();
        mockSCIMApiClient.Setup(c => c.GetSCIMMemberByExternalId("missing-user-id")).ReturnsAsync((SCIMUser?)null);

        var options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_" + Guid.NewGuid())
            .Options;

        var context = new ConfluxContext(options);

        var service = new SessionMappingService(context, mockSCIMApiClient.Object, mockFeatureManager.Object);

        var member = new GroupMember
        {
            SCIMId = "missing-user-id",
            DisplayName = "Missing User"
        };

        var collaborationGroup = new Group
        {
            Id = Guid.NewGuid().ToString(),
            SCIMId = "group-id-1",
            DisplayName = "Test Group",
            Description = "Test Description",
            Created = DateTime.UtcNow,
            Urn = "urn:test:group",
            ExternalId = "ext-id-1",
            Members = new List<GroupMember> { member }
        };

        var userSession = new UserSession
        {
            Email = "test@example.com",
            SRAMId = "sram-id-1",
            Collaborations = new List<Collaboration>
            {
                new Collaboration
                {
                    Organization = "test-org",
                    CollaborationGroup = collaborationGroup,
                    Groups = new List<Group>()
                }
            }
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
        mockFeatureManager.Setup(m => m.IsEnabledAsync("SRAMAuthentication", CancellationToken.None)).ReturnsAsync(true);

        var mockSCIMApiClient = new Mock<ISCIMApiClient>();
        var scimUser = new SCIMUser
        {
            Id = "user-id-1",
            DisplayName = "Test User",
            UserName = "testuser",
            Name = new SCIMName
            {
                GivenName = "Test",
                FamilyName = "User"
            },
            Emails = new List<SCIMEmail>
            {
                new SCIMEmail { Value = "test@example.com" }
            }
        };
        mockSCIMApiClient.Setup(c => c.GetSCIMMemberByExternalId("member-id-1")).ReturnsAsync(scimUser);

        var options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_" + Guid.NewGuid())
            .Options;

        var context = new ConfluxContext(options);

        // Add existing user without SRAM ID
        context.Users.Add(new User
        {
            SCIMId = "user-id-1",
            Name = "Existing User",
            Email = "test@example.com"
        });
        await context.SaveChangesAsync();

        var service = new SessionMappingService(context, mockSCIMApiClient.Object, mockFeatureManager.Object);

        var member = new GroupMember
        {
            SCIMId = "member-id-1",
            DisplayName = "Test Member"
        };

        var collaborationGroup = new Group
        {
            Id = Guid.NewGuid().ToString(),
            SCIMId = "group-id-1",
            DisplayName = "Test Group",
            Description = "Test Description",
            Created = DateTime.UtcNow,
            Urn = "urn:test:group",
            ExternalId = "ext-id-1",
            Members = new List<GroupMember> { member }
        };

        var userSession = new UserSession
        {
            Email = "test@example.com",
            SRAMId = "sram-id-1",
            Collaborations = new List<Collaboration>
            {
                new Collaboration
                {
                    Organization = "test-org",
                    CollaborationGroup = collaborationGroup,
                    Groups = new List<Group>()
                }
            }
        };

        // Act
        await service.CollectSessionData(userSession);

        // Assert
        Assert.Single(context.Users);
        var user = context.Users.First();
        Assert.Equal("sram-id-1", user.SRAMId);
    }

    [Fact]
    public async Task CollectSessionData_WithExistingUserWithDifferentEmail_DoesNotUpdateSRAMId()
    {
        // Arrange
        var mockFeatureManager = new Mock<IVariantFeatureManager>();
        mockFeatureManager.Setup(m => m.IsEnabledAsync("SRAMAuthentication", CancellationToken.None)).ReturnsAsync(true);

        var mockSCIMApiClient = new Mock<ISCIMApiClient>();
        var scimUser = new SCIMUser
        {
            Id = "user-id-1",
            DisplayName = "Test User",
            UserName = "testuser",
            Name = new SCIMName
            {
                GivenName = "Test",
                FamilyName = "User"
            },
            Emails = new List<SCIMEmail>
            {
                new SCIMEmail { Value = "different@example.com" }
            }
        };
        mockSCIMApiClient.Setup(c => c.GetSCIMMemberByExternalId("member-id-1")).ReturnsAsync(scimUser);

        var options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_" + Guid.NewGuid())
            .Options;

        var context = new ConfluxContext(options);

        // Add existing user with different email
        context.Users.Add(new User
        {
            SCIMId = "user-id-1",
            Name = "Existing User",
            Email = "different@example.com"
        });
        await context.SaveChangesAsync();

        var service = new SessionMappingService(context, mockSCIMApiClient.Object, mockFeatureManager.Object);

        var member = new GroupMember
        {
            SCIMId = "member-id-1",
            DisplayName = "Test Member"
        };

        var collaborationGroup = new Group
        {
            Id = Guid.NewGuid().ToString(),
            SCIMId = "group-id-1",
            DisplayName = "Test Group",
            Description = "Test Description",
            Created = DateTime.UtcNow,
            Urn = "urn:test:group",
            ExternalId = "ext-id-1",
            Members = new List<GroupMember> { member }
        };

        var userSession = new UserSession
        {
            Email = "test@example.com",
            SRAMId = "sram-id-1",
            Collaborations = new List<Collaboration>
            {
                new Collaboration
                {
                    Organization = "test-org",
                    CollaborationGroup = collaborationGroup,
                    Groups = new List<Group>()
                }
            }
        };

        // Act
        await service.CollectSessionData(userSession);

        // Assert
        Assert.Single(context.Users);
        var user = context.Users.First();
        Assert.Null(user.SRAMId);
    }
}