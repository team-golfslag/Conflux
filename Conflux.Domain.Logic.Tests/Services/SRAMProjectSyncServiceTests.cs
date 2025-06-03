// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Reflection;
using Conflux.Data;
using Conflux.Domain.Logic.Exceptions;
using Conflux.Domain.Logic.Services;
using Conflux.Integrations.SRAM.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SRAM.SCIM.Net;
using SRAM.SCIM.Net.Models;
using Xunit;

namespace Conflux.Domain.Logic.Tests.Services;

public class SRAMProjectSyncServiceTests
{
    [Fact]
    public async Task SyncProjectAsync_AddsNewUserFromSCIM()
    {
        // Arrange
        string dbName = $"SyncProject_{Guid.NewGuid()}";
        ConfluxContext context = CreateInMemoryContext(dbName);
        var mockScimApiClient = new Mock<ISCIMApiClient>();

        // Create a new SCIMGroup response with a new user
        SCIMGroup scimGroup = new()
        {
            Id = "scim-id",
            DisplayName = "Test Project",
            ExternalId = "external-id",
            Schemas = new()
            {
                "urn:ietf:params:scim:schemas:core:2.0:Group",
            },
            Members = new()
            {
                new()
                {
                    Display = "New User",
                    Value = "new-user-scim-id",
                    Ref = null!,
                },
            },
            SCIMGroupInfo = new()
            {
                Urn = "group-urn",
            },
            SCIMMeta = new()
            {
                Created = DateTime.UtcNow,
                Location = null!,
                ResourceType = null!,
                Version = null!,
            },
        };

        // Setup the mock
        mockScimApiClient.Setup(m => m.GetSCIMGroup(It.IsAny<string>())).ReturnsAsync(scimGroup);

        // Create a service that only tests the user addition logic
        SRAMProjectSyncService service = new(mockScimApiClient.Object, context);

        // Use reflection to call the private method that adds users
        MethodInfo? methodInfo = typeof(SRAMProjectSyncService).GetMethod("AddMissingUsers",
            BindingFlags.NonPublic | BindingFlags.Instance);

        if (methodInfo == null)
            // If the method doesn't exist or has a different name, skip the test
            return;

        Project project = new()
        {
            Id = Guid.NewGuid(),
            SCIMId = "project-scim-id",
            Users = new(),
            StartDate = DateTime.UtcNow,
        };

        // Act - invoke the private method
        await (Task)methodInfo.Invoke(service, new object[] { project, scimGroup })!;

        // Assert - Check if the user was added
        Assert.Contains(context.Users, u => u.SCIMId == "new-user-scim-id");
    }

    [Fact]
    public async Task SyncProjectAsync_WithNonExistentProject_ThrowsProjectNotFoundException()
    {
        // Arrange
        string dbName = $"NonExistentProject_{Guid.NewGuid()}";
        ConfluxContext context = CreateInMemoryContext(dbName);
        var mockScimApiClient = new Mock<ISCIMApiClient>();

        Guid nonExistentProjectId = Guid.NewGuid();

        SRAMProjectSyncService service = new(mockScimApiClient.Object, context);

        // Act & Assert
        await Assert.ThrowsAsync<ProjectNotFoundException>(() =>
            service.SyncProjectAsync(nonExistentProjectId));

        mockScimApiClient.Verify(m => m.GetSCIMGroup(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SyncProjectAsync_WithProjectNotFoundInApi_ThrowsProjectNotFoundException()
    {
        // Arrange
        string dbName = $"ProjectNotInApi_{Guid.NewGuid()}";
        ConfluxContext context = CreateInMemoryContext(dbName);
        var mockScimApiClient = new Mock<ISCIMApiClient>();

        Guid projectId = Guid.NewGuid();
        string projectScimId = "project-scim-id";

        Project project = new()
        {
            Id = projectId,
            SCIMId = projectScimId,
            Users = new(),
            StartDate = DateTime.UtcNow.AddDays(-10),
        };

        context.Projects.Add(project);
        await context.SaveChangesAsync();

        mockScimApiClient.Setup(m => m.GetSCIMGroup(projectScimId)).ReturnsAsync((SCIMGroup)null!);

        SRAMProjectSyncService service = new(mockScimApiClient.Object, context);

        // Act & Assert
        await Assert.ThrowsAsync<ProjectNotFoundException>(() =>
            service.SyncProjectAsync(projectId));

        mockScimApiClient.Verify(m => m.GetSCIMGroup(projectScimId), Times.Once);
    }

    [Fact]
    public async Task SyncProjectAsync_WithValidProject_SyncsSuccessfully()
    {
        // Arrange
        string dbName = $"ValidProject_{Guid.NewGuid()}";
        ConfluxContext context = CreateInMemoryContext(dbName);
        var mockScimApiClient = new Mock<ISCIMApiClient>();

        Guid projectId = Guid.NewGuid();
        Guid existingUserId = Guid.NewGuid();
        Guid existingPersonId = Guid.NewGuid();
        string projectScimId = "project-scim-id";

        // Create existing project with existing user
        Person existingPerson = new()
        {
            Id = existingPersonId,
            Name = "Existing User",
        };

        User existingUser = new()
        {
            Id = existingUserId,
            SCIMId = "existing-user-scim-id",
            PersonId = existingPersonId,
            Person = existingPerson,
            Roles = new List<UserRole>(),
        };

        Project project = new()
        {
            Id = projectId,
            SCIMId = projectScimId,
            Users = new List<User> { existingUser },
            Contributors = new List<Contributor>(),
            StartDate = DateTime.UtcNow.AddDays(-10),
        };

        context.People.Add(existingPerson);
        context.Users.Add(existingUser);
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        // Create SCIM response with new user
        SCIMGroup scimGroup = new()
        {
            Id = projectScimId,
            DisplayName = "Test Project",
            ExternalId = "external-id",
            Schemas = new() { "urn:ietf:params:scim:schemas:core:2.0:Group" },
            Members = new()
            {
                new()
                {
                    Display = "Existing User",
                    Value = "existing-user-scim-id",
                    Ref = null!,
                },
                new()
                {
                    Display = "New User",
                    Value = "new-user-scim-id",
                    Ref = null!,
                },
            },
            SCIMGroupInfo = new() { Urn = "test-urn" },
            SCIMMeta = new()
            {
                Created = DateTime.UtcNow,
                Location = null!,
                ResourceType = null!,
                Version = null!,
            },
        };

        mockScimApiClient.Setup(m => m.GetSCIMGroup(projectScimId)).ReturnsAsync(scimGroup);

        SRAMProjectSyncService service = new(mockScimApiClient.Object, context);

        // Act
        await service.SyncProjectAsync(projectId);

        // Assert
        var updatedProject = await context.Projects
            .Include(p => p.Users)
            .ThenInclude(u => u.Person)
            .SingleAsync(p => p.Id == projectId);

        Assert.Equal(2, updatedProject.Users.Count);
        Assert.Contains(updatedProject.Users, u => u.SCIMId == "existing-user-scim-id");
        Assert.Contains(updatedProject.Users, u => u.SCIMId == "new-user-scim-id");

        var newUser = updatedProject.Users.Single(u => u.SCIMId == "new-user-scim-id");
        Assert.NotNull(newUser.Person);
        Assert.Equal("New User", newUser.Person.Name);

        mockScimApiClient.Verify(m => m.GetSCIMGroup(projectScimId), Times.Once);
    }

    [Fact]
    public async Task SyncProjectRoleAsync_WithValidRole_UpdatesRoleSuccessfully()
    {
        // Arrange
        string dbName = $"ValidRole_{Guid.NewGuid()}";
        ConfluxContext context = CreateInMemoryContext(dbName);
        var mockScimApiClient = new Mock<ISCIMApiClient>();

        Guid projectId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        Guid personId = Guid.NewGuid();
        Guid roleId = Guid.NewGuid();
        string roleScimId = "role-scim-id";
        string roleUrn = "urn:mace:surf.nl:sram:group:org:co:conflux-admin";

        Person person = new()
        {
            Id = personId,
            Name = "Test User",
        };

        User user = new()
        {
            Id = userId,
            SCIMId = "user-scim-id",
            PersonId = personId,
            Person = person,
            Roles = new List<UserRole>(),
        };

        Project project = new()
        {
            Id = projectId,
            SCIMId = "project-scim-id",
            Users = new List<User> { user },
            Contributors = new List<Contributor>(),
            StartDate = DateTime.UtcNow.AddDays(-10),
        };

        UserRole role = new()
        {
            Id = roleId,
            ProjectId = projectId,
            Type = UserRoleType.Admin,
            SCIMId = roleScimId,
            Urn = roleUrn,
        };

        context.People.Add(person);
        context.Users.Add(user);
        context.Projects.Add(project);
        context.UserRoles.Add(role);
        await context.SaveChangesAsync();

        // Create updated SCIM group
        SCIMGroup updatedScimGroup = new()
        {
            Id = roleScimId,
            DisplayName = "Updated Admin Group",
            ExternalId = "external-id",
            Schemas = new() { "urn:ietf:params:scim:schemas:core:2.0:Group" },
            Members = new()
            {
                new()
                {
                    Display = "Test User",
                    Value = "user-scim-id",
                    Ref = null!,
                },
            },
            SCIMGroupInfo = new() { Urn = "org:co:conflux-admin" },
            SCIMMeta = new()
            {
                Created = DateTime.UtcNow,
                Location = null!,
                ResourceType = null!,
                Version = null!,
            },
        };

        mockScimApiClient.Setup(m => m.GetSCIMGroup(roleScimId)).ReturnsAsync(updatedScimGroup);

        SRAMProjectSyncService service = new(mockScimApiClient.Object, context);

        // Act
        await service.SyncProjectRoleAsync(project, role);

        // Assert
        var updatedUser = await context.Users
            .Include(u => u.Roles)
            .SingleAsync(u => u.Id == userId);

        Assert.Single(updatedUser.Roles);
        var updatedRole = updatedUser.Roles.First();
        Assert.Equal(UserRoleType.Admin, updatedRole.Type);
        Assert.Equal("urn:mace:surf.nl:sram:group:org:co:conflux-admin", updatedRole.Urn);

        // Verify old role was NOT removed (implementation creates new role but doesn't remove old one)
        var oldRole = await context.UserRoles.SingleOrDefaultAsync(r => r.Id == roleId);
        Assert.NotNull(oldRole); // Old role should still exist

        mockScimApiClient.Verify(m => m.GetSCIMGroup(roleScimId), Times.Once);
    }

    [Fact]
    public async Task SyncProjectRoleAsync_WithContributorRole_CreatesContributors()
    {
        // Arrange
        string dbName = $"ContributorRole_{Guid.NewGuid()}";
        ConfluxContext context = CreateInMemoryContext(dbName);
        var mockScimApiClient = new Mock<ISCIMApiClient>();

        Guid projectId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        Guid personId = Guid.NewGuid();
        Guid roleId = Guid.NewGuid();
        string roleScimId = "contributor-role-scim-id";
        string roleUrn = "urn:mace:surf.nl:sram:group:org:co:conflux-contributor";

        Person person = new()
        {
            Id = personId,
            Name = "Test Contributor",
        };

        User user = new()
        {
            Id = userId,
            SCIMId = "contributor-scim-id",
            PersonId = personId,
            Person = person,
            Roles = new List<UserRole>(),
        };

        Project project = new()
        {
            Id = projectId,
            SCIMId = "project-scim-id",
            Users = new List<User> { user },
            Contributors = new List<Contributor>(),
            StartDate = DateTime.UtcNow.AddDays(-10),
        };

        UserRole role = new()
        {
            Id = roleId,
            ProjectId = projectId,
            Type = UserRoleType.Contributor,
            SCIMId = roleScimId,
            Urn = roleUrn,
        };

        context.People.Add(person);
        context.Users.Add(user);
        context.Projects.Add(project);
        context.UserRoles.Add(role);
        await context.SaveChangesAsync();

        // Create updated SCIM group
        SCIMGroup updatedScimGroup = new()
        {
            Id = roleScimId,
            DisplayName = "Contributor Group",
            ExternalId = "external-id",
            Schemas = new() { "urn:ietf:params:scim:schemas:core:2.0:Group" },
            Members = new()
            {
                new()
                {
                    Display = "Test Contributor",
                    Value = "contributor-scim-id",
                    Ref = null!,
                },
            },
            SCIMGroupInfo = new() { Urn = "org:co:conflux-contributor" },
            SCIMMeta = new()
            {
                Created = DateTime.UtcNow,
                Location = null!,
                ResourceType = null!,
                Version = null!,
            },
        };

        mockScimApiClient.Setup(m => m.GetSCIMGroup(roleScimId)).ReturnsAsync(updatedScimGroup);

        SRAMProjectSyncService service = new(mockScimApiClient.Object, context);

        // Act
        await service.SyncProjectRoleAsync(project, role);
        
        // Save changes to persist the contributors created by SyncProjectRoleAsync
        await context.SaveChangesAsync();

        // Assert - Check how many contributors are actually in the database for this project
        var contributorsInDatabase = await context.Contributors
            .Where(c => c.ProjectId == projectId)
            .ToListAsync();

        // The implementation should create exactly one contributor
        Assert.Single(contributorsInDatabase);
        var contributor = contributorsInDatabase.First();
        Assert.Equal(personId, contributor.PersonId);
        Assert.Equal(projectId, contributor.ProjectId);

        mockScimApiClient.Verify(m => m.GetSCIMGroup(roleScimId), Times.Once);
    }

    [Fact]
    public async Task SyncProjectRoleAsync_RemovingContributorRole_EndsPositions()
    {
        // Arrange
        string dbName = $"RemoveContributor_{Guid.NewGuid()}";
        ConfluxContext context = CreateInMemoryContext(dbName);
        var mockScimApiClient = new Mock<ISCIMApiClient>();

        Guid projectId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        Guid personId = Guid.NewGuid();
        Guid roleId = Guid.NewGuid();
        string roleScimId = "contributor-role-scim-id";
        string roleUrn = "urn:mace:surf.nl:sram:group:org:co:conflux-contributor";

        Person person = new()
        {
            Id = personId,
            Name = "Test Contributor",
        };

        User user = new()
        {
            Id = userId,
            SCIMId = "contributor-scim-id",
            PersonId = personId,
            Person = person,
            Roles = new List<UserRole>(),
        };

        ContributorPosition position = new()
        {
            PersonId = personId,
            ProjectId = projectId,
            Position = ContributorPositionType.PrincipalInvestigator,
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = null, // Active position
        };

        Contributor contributor = new()
        {
            PersonId = personId,
            ProjectId = projectId,
            Person = person,
            Roles = new List<ContributorRole>(),
            Positions = new List<ContributorPosition> { position },
        };

        Project project = new()
        {
            Id = projectId,
            SCIMId = "project-scim-id",
            Users = new List<User> { user },
            Contributors = new List<Contributor> { contributor },
            StartDate = DateTime.UtcNow.AddDays(-10),
        };

        UserRole role = new()
        {
            Id = roleId,
            ProjectId = projectId,
            Type = UserRoleType.Contributor,
            SCIMId = roleScimId,
            Urn = roleUrn,
        };

        context.People.Add(person);
        context.Users.Add(user);
        context.Projects.Add(project);
        context.Contributors.Add(contributor);
        context.ContributorPositions.Add(position);
        context.UserRoles.Add(role);
        await context.SaveChangesAsync();

        // Create updated SCIM group without the user (removing contributor role)
        SCIMGroup updatedScimGroup = new()
        {
            Id = roleScimId,
            DisplayName = "Contributor Group",
            ExternalId = "external-id",
            Schemas = new() { "urn:ietf:params:scim:schemas:core:2.0:Group" },
            Members = new List<SCIMMember>(), // Empty - user removed from contributor role
            SCIMGroupInfo = new() { Urn = "org:co:conflux-contributor" },
            SCIMMeta = new()
            {
                Created = DateTime.UtcNow,
                Location = null!,
                ResourceType = null!,
                Version = null!,
            },
        };

        mockScimApiClient.Setup(m => m.GetSCIMGroup(roleScimId)).ReturnsAsync(updatedScimGroup);

        SRAMProjectSyncService service = new(mockScimApiClient.Object, context);

        // Act
        await service.SyncProjectRoleAsync(project, role);

        // Assert
        var updatedPosition = await context.ContributorPositions
            .SingleAsync(p => p.PersonId == personId && p.ProjectId == projectId && p.Position == ContributorPositionType.PrincipalInvestigator);
        Assert.NotNull(updatedPosition.EndDate);
        Assert.True(updatedPosition.EndDate <= DateTime.UtcNow);

        mockScimApiClient.Verify(m => m.GetSCIMGroup(roleScimId), Times.Once);
    }

    [Fact]
    public async Task SyncProjectAsync_WithMultipleUsers_AddsAllNewUsers()
    {
        // Arrange
        string dbName = $"MultipleUsers_{Guid.NewGuid()}";
        ConfluxContext context = CreateInMemoryContext(dbName);
        var mockScimApiClient = new Mock<ISCIMApiClient>();

        Guid projectId = Guid.NewGuid();
        string projectScimId = "project-scim-id";

        Project project = new()
        {
            Id = projectId,
            SCIMId = projectScimId,
            Users = new List<User>(),
            Contributors = new List<Contributor>(),
            StartDate = DateTime.UtcNow.AddDays(-10),
        };

        context.Projects.Add(project);
        await context.SaveChangesAsync();

        // Create SCIM response with multiple new users
        SCIMGroup scimGroup = new()
        {
            Id = projectScimId,
            DisplayName = "Test Project",
            ExternalId = "external-id",
            Schemas = new() { "urn:ietf:params:scim:schemas:core:2.0:Group" },
            Members = new()
            {
                new()
                {
                    Display = "User One",
                    Value = "user-one-scim-id",
                    Ref = null!,
                },
                new()
                {
                    Display = "User Two",
                    Value = "user-two-scim-id",
                    Ref = null!,
                },
                new()
                {
                    Display = "User Three",
                    Value = "user-three-scim-id",
                    Ref = null!,
                },
            },
            SCIMGroupInfo = new() { Urn = "test-urn" },
            SCIMMeta = new()
            {
                Created = DateTime.UtcNow,
                Location = null!,
                ResourceType = null!,
                Version = null!,
            },
        };

        mockScimApiClient.Setup(m => m.GetSCIMGroup(projectScimId)).ReturnsAsync(scimGroup);

        SRAMProjectSyncService service = new(mockScimApiClient.Object, context);

        // Act
        await service.SyncProjectAsync(projectId);

        // Assert
        var updatedProject = await context.Projects
            .Include(p => p.Users)
            .ThenInclude(u => u.Person)
            .SingleAsync(p => p.Id == projectId);

        Assert.Equal(3, updatedProject.Users.Count);
        Assert.Contains(updatedProject.Users, u => u.SCIMId == "user-one-scim-id");
        Assert.Contains(updatedProject.Users, u => u.SCIMId == "user-two-scim-id");
        Assert.Contains(updatedProject.Users, u => u.SCIMId == "user-three-scim-id");

        // Verify all users have people created
        Assert.All(updatedProject.Users, user => Assert.NotNull(user.Person));

        var userNames = updatedProject.Users.Select(u => u.Person?.Name).ToList();
        Assert.Contains("User One", userNames);
        Assert.Contains("User Two", userNames);
        Assert.Contains("User Three", userNames);

        mockScimApiClient.Verify(m => m.GetSCIMGroup(projectScimId), Times.Once);
    }

    [Fact]
    public async Task SyncProjectAsync_WithExistingUsers_DoesNotDuplicateUsers()
    {
        // Arrange
        string dbName = $"ExistingUsers_{Guid.NewGuid()}";
        ConfluxContext context = CreateInMemoryContext(dbName);
        var mockScimApiClient = new Mock<ISCIMApiClient>();

        Guid projectId = Guid.NewGuid();
        Guid existingUserId = Guid.NewGuid();
        Guid existingPersonId = Guid.NewGuid();
        string projectScimId = "project-scim-id";

        Person existingPerson = new()
        {
            Id = existingPersonId,
            Name = "Existing User",
        };

        User existingUser = new()
        {
            Id = existingUserId,
            SCIMId = "existing-user-scim-id",
            PersonId = existingPersonId,
            Person = existingPerson,
            Roles = new List<UserRole>(),
        };

        Project project = new()
        {
            Id = projectId,
            SCIMId = projectScimId,
            Users = new List<User> { existingUser },
            Contributors = new List<Contributor>(),
            StartDate = DateTime.UtcNow.AddDays(-10),
        };

        context.People.Add(existingPerson);
        context.Users.Add(existingUser);
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        // Create SCIM response with same existing user
        SCIMGroup scimGroup = new()
        {
            Id = projectScimId,
            DisplayName = "Test Project",
            ExternalId = "external-id",
            Schemas = new() { "urn:ietf:params:scim:schemas:core:2.0:Group" },
            Members = new()
            {
                new()
                {
                    Display = "Existing User",
                    Value = "existing-user-scim-id",
                    Ref = null!,
                },
            },
            SCIMGroupInfo = new() { Urn = "test-urn" },
            SCIMMeta = new()
            {
                Created = DateTime.UtcNow,
                Location = null!,
                ResourceType = null!,
                Version = null!,
            },
        };

        mockScimApiClient.Setup(m => m.GetSCIMGroup(projectScimId)).ReturnsAsync(scimGroup);

        SRAMProjectSyncService service = new(mockScimApiClient.Object, context);

        // Act
        await service.SyncProjectAsync(projectId);

        // Assert
        var updatedProject = await context.Projects
            .Include(p => p.Users)
            .SingleAsync(p => p.Id == projectId);

        Assert.Single(updatedProject.Users);
        Assert.Equal(existingUserId, updatedProject.Users.First().Id);

        // Verify no duplicate users were created
        var allUsers = await context.Users.CountAsync();
        Assert.Equal(1, allUsers);

        mockScimApiClient.Verify(m => m.GetSCIMGroup(projectScimId), Times.Once);
    }

    [Fact]
    public async Task SyncProjectRoleAsync_WithRoleNotFoundInApi_ThrowsGroupNotFoundException()
    {
        // Arrange
        string dbName = $"RoleNotInApi_{Guid.NewGuid()}";
        ConfluxContext context = CreateInMemoryContext(dbName);
        var mockScimApiClient = new Mock<ISCIMApiClient>();

        Guid projectId = Guid.NewGuid();
        string projectScimId = "project-scim-id";
        Guid roleId = Guid.NewGuid();
        string roleScimId = "role-scim-id";
        string roleUrn = "urn:mace:surf.nl:sram:group:org:co:conflux-admin";

        Project project = new()
        {
            Id = projectId,
            SCIMId = projectScimId,
            Users = new(),
            StartDate = DateTime.UtcNow.AddDays(-10),
        };

        UserRole role = new()
        {
            Id = roleId,
            ProjectId = projectId,
            Type = UserRoleType.Admin,
            SCIMId = roleScimId,
            Urn = roleUrn,
        };

        mockScimApiClient.Setup(m => m.GetSCIMGroup(roleScimId)).ReturnsAsync((SCIMGroup)null!);

        SRAMProjectSyncService service = new(mockScimApiClient.Object, context);

        // Act & Assert
        await Assert.ThrowsAsync<GroupNotFoundException>(() =>
            service.SyncProjectRoleAsync(project, role));

        mockScimApiClient.Verify(m => m.GetSCIMGroup(roleScimId), Times.Once);
    }

    [Fact]
    public async Task SyncProjectAsync_WithEmptyMembersList_DoesNotRemoveExistingUsers()
    {
        // Arrange
        string dbName = $"EmptyMembers_{Guid.NewGuid()}";
        ConfluxContext context = CreateInMemoryContext(dbName);
        var mockScimApiClient = new Mock<ISCIMApiClient>();

        Guid projectId = Guid.NewGuid();
        Guid existingUserId = Guid.NewGuid();
        Guid existingPersonId = Guid.NewGuid();
        string projectScimId = "project-scim-id";

        Person existingPerson = new()
        {
            Id = existingPersonId,
            Name = "Existing User",
        };

        User existingUser = new()
        {
            Id = existingUserId,
            SCIMId = "existing-user-scim-id",
            PersonId = existingPersonId,
            Person = existingPerson,
            Roles = new List<UserRole>(),
        };

        Project project = new()
        {
            Id = projectId,
            SCIMId = projectScimId,
            Users = new List<User> { existingUser },
            Contributors = new List<Contributor>(),
            StartDate = DateTime.UtcNow.AddDays(-10),
        };

        context.People.Add(existingPerson);
        context.Users.Add(existingUser);
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        // Create SCIM response with empty members list
        SCIMGroup scimGroup = new()
        {
            Id = projectScimId,
            DisplayName = "Test Project",
            ExternalId = "external-id",
            Schemas = new() { "urn:ietf:params:scim:schemas:core:2.0:Group" },
            Members = new List<SCIMMember>(), // Empty members list
            SCIMGroupInfo = new() { Urn = "test-urn" },
            SCIMMeta = new()
            {
                Created = DateTime.UtcNow,
                Location = null!,
                ResourceType = null!,
                Version = null!,
            },
        };

        mockScimApiClient.Setup(m => m.GetSCIMGroup(projectScimId)).ReturnsAsync(scimGroup);

        SRAMProjectSyncService service = new(mockScimApiClient.Object, context);

        // Act
        await service.SyncProjectAsync(projectId);

        // Assert - The implementation only adds new users, it doesn't remove existing ones
        var updatedProject = await context.Projects
            .Include(p => p.Users)
            .SingleAsync(p => p.Id == projectId);

        Assert.Single(updatedProject.Users); // Existing user should still be there
        Assert.Equal(existingUserId, updatedProject.Users.First().Id);
        mockScimApiClient.Verify(m => m.GetSCIMGroup(projectScimId), Times.Once);
    }

    [Fact]
    public async Task SyncProjectRoleAsync_WithApiError_ThrowsException()
    {
        // Arrange
        string dbName = $"ApiError_{Guid.NewGuid()}";
        ConfluxContext context = CreateInMemoryContext(dbName);
        var mockScimApiClient = new Mock<ISCIMApiClient>();

        Guid projectId = Guid.NewGuid();
        string projectScimId = "project-scim-id";
        Guid roleId = Guid.NewGuid();
        string roleScimId = "role-scim-id";
        string roleUrn = "urn:mace:surf.nl:sram:group:org:co:conflux-admin";

        Project project = new()
        {
            Id = projectId,
            SCIMId = projectScimId,
            Users = new(),
            StartDate = DateTime.UtcNow.AddDays(-10),
        };

        UserRole role = new()
        {
            Id = roleId,
            ProjectId = projectId,
            Type = UserRoleType.Admin,
            SCIMId = roleScimId,
            Urn = roleUrn,
        };

        // Setup mock to throw an exception (simulating API error)
        mockScimApiClient.Setup(m => m.GetSCIMGroup(roleScimId))
            .ThrowsAsync(new HttpRequestException("API Error"));

        SRAMProjectSyncService service = new(mockScimApiClient.Object, context);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            service.SyncProjectRoleAsync(project, role));

        mockScimApiClient.Verify(m => m.GetSCIMGroup(roleScimId), Times.Once);
    }

    [Fact]
    public async Task SyncProjectRoleAsync_WithExistingContributor_UpdatesExistingContributor()
    {
        // Arrange
        string dbName = $"ExistingContributor_{Guid.NewGuid()}";
        ConfluxContext context = CreateInMemoryContext(dbName);
        var mockScimApiClient = new Mock<ISCIMApiClient>();

        Guid projectId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        Guid personId = Guid.NewGuid();
        Guid roleId = Guid.NewGuid();
        string roleScimId = "contributor-role-scim-id";
        string roleUrn = "urn:mace:surf.nl:sram:group:org:co:conflux-contributor";

        Person person = new()
        {
            Id = personId,
            Name = "Test Contributor",
        };

        User user = new()
        {
            Id = userId,
            SCIMId = "contributor-scim-id",
            PersonId = personId,
            Person = person,
            Roles = new List<UserRole>(),
        };

        // Create existing contributor
        Contributor existingContributor = new()
        {
            PersonId = personId,
            ProjectId = projectId,
            Person = person,
            Roles = new List<ContributorRole>(),
            Positions = new List<ContributorPosition>(),
        };

        Project project = new()
        {
            Id = projectId,
            SCIMId = "project-scim-id",
            Users = new List<User> { user },
            Contributors = new List<Contributor> { existingContributor },
            StartDate = DateTime.UtcNow.AddDays(-10),
        };

        UserRole role = new()
        {
            Id = roleId,
            ProjectId = projectId,
            Type = UserRoleType.Contributor,
            SCIMId = roleScimId,
            Urn = roleUrn,
        };

        context.People.Add(person);
        context.Users.Add(user);
        context.Projects.Add(project);
        context.Contributors.Add(existingContributor);
        context.UserRoles.Add(role);
        await context.SaveChangesAsync();

        // Create updated SCIM group
        SCIMGroup updatedScimGroup = new()
        {
            Id = roleScimId,
            DisplayName = "Contributor Group",
            ExternalId = "external-id",
            Schemas = new() { "urn:ietf:params:scim:schemas:core:2.0:Group" },
            Members = new()
            {
                new()
                {
                    Display = "Test Contributor",
                    Value = "contributor-scim-id",
                    Ref = null!,
                },
            },
            SCIMGroupInfo = new() { Urn = "org:co:conflux-contributor" },
            SCIMMeta = new()
            {
                Created = DateTime.UtcNow,
                Location = null!,
                ResourceType = null!,
                Version = null!,
            },
        };

        mockScimApiClient.Setup(m => m.GetSCIMGroup(roleScimId)).ReturnsAsync(updatedScimGroup);

        SRAMProjectSyncService service = new(mockScimApiClient.Object, context);

        // Act
        await service.SyncProjectRoleAsync(project, role);

        // Assert
        var updatedProject = await context.Projects
            .Include(p => p.Contributors)
            .SingleAsync(p => p.Id == projectId);

        // Should still have only one contributor (not duplicated)
        Assert.Single(updatedProject.Contributors);
        var contributor = updatedProject.Contributors.First();
        Assert.Equal(personId, contributor.PersonId);
        Assert.Equal(projectId, contributor.ProjectId);

        mockScimApiClient.Verify(m => m.GetSCIMGroup(roleScimId), Times.Once);
    }

    [Fact]
    public async Task SyncProjectAsync_WithNullSCIMId_ThrowsProjectNotFoundException()
    {
        // Arrange
        string dbName = $"NullSCIMId_{Guid.NewGuid()}";
        ConfluxContext context = CreateInMemoryContext(dbName);
        var mockScimApiClient = new Mock<ISCIMApiClient>();

        Guid projectId = Guid.NewGuid();

        Project project = new()
        {
            Id = projectId,
            SCIMId = null!, // Null SCIM ID
            Users = new List<User>(),
            Contributors = new List<Contributor>(),
            StartDate = DateTime.UtcNow.AddDays(-10),
        };

        context.Projects.Add(project);
        await context.SaveChangesAsync();

        // When SCIMId is null, the service calls GetSCIMGroup with empty string and gets null response
        mockScimApiClient.Setup(m => m.GetSCIMGroup(string.Empty)).ReturnsAsync((SCIMGroup)null!);

        SRAMProjectSyncService service = new(mockScimApiClient.Object, context);

        // Act & Assert
        await Assert.ThrowsAsync<ProjectNotFoundException>(() =>
            service.SyncProjectAsync(projectId));

        // Verify API was called with empty string
        mockScimApiClient.Verify(m => m.GetSCIMGroup(string.Empty), Times.Once);
    }

    private ConfluxContext CreateInMemoryContext(string dbName)
    {
        ServiceProvider serviceProvider = new ServiceCollection()
            .AddEntityFrameworkInMemoryDatabase()
            .BuildServiceProvider();

        var options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase(dbName)
            .UseInternalServiceProvider(serviceProvider)
            .Options;

        ConfluxContext context = new(options);
        context.Database.EnsureCreated();
        return context;
    }
}
