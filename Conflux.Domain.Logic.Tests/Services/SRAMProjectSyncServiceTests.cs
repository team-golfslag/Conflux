// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
//
// © Copyright Utrecht University (Department of Information and Computing Sciences)

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

public class SRAMProjectSyncServiceTests : IDisposable
{
    private readonly ConfluxContext _context;
    private readonly Mock<ISCIMApiClient> _mockScimApiClient;
    private readonly SRAMProjectSyncService _service;

    public SRAMProjectSyncServiceTests()
    {
        ServiceProvider serviceProvider = new ServiceCollection()
            .AddEntityFrameworkInMemoryDatabase()
            .BuildServiceProvider();

        DbContextOptions<ConfluxContext> options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .UseInternalServiceProvider(serviceProvider)
            .Options;

        _context = new(options);
        _context.Database.EnsureCreated();

        _mockScimApiClient = new();
        _service = new(_mockScimApiClient.Object, _context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task<(User User, Person Person)> CreateAndSaveUserAsync(string name, string scimId)
    {
        Person person = new()
            { Id = Guid.NewGuid(), Name = name };
        User user = new()
        {
            Id = Guid.NewGuid(),
            SCIMId = scimId,
            Person = person,
            PersonId = person.Id,
        };
        person.User = user;

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return (user, person);
    }

    private async Task<Project> CreateAndSaveProjectAsync(string scimId, List<User>? users = null)
    {
        Project project = new()
        {
            Id = Guid.NewGuid(),
            SCIMId = scimId,
            Users = users ?? [],
            StartDate = DateTime.UtcNow.AddDays(-10)
        };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();
        return project;
    }

    private static SCIMGroup CreateScimGroup(string id, IEnumerable<SCIMMember> members) =>
        new()
        {
            Id = id,
            DisplayName = "Test Group",
            Members = members.ToList(),
            SCIMGroupInfo = new()
            {
                Urn = "test-urn"
            },
            ExternalId = Guid.NewGuid().ToString(),
            SCIMMeta = new()
            {
                Created = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                ResourceType = "Group",
                Location = $"https://api.sram.surf.nl/scim/v2/Groups/{id}",
                Version = "1.0"
            },
            Schemas = ["urn:ietf:params:scim:schemas:core:2.0:Group"]
        };

    private static SCIMMember CreateScimMember(string displayName, string value) =>
        new()
        {
            Display = displayName,
            Value = value,
            Ref = $"https://api.sram.surf.nl/scim/v2/Users/{value}",
        };

    [Fact]
    public async Task SyncProjectAsync_WithNewMemberInScimGroup_AddsUserToProject()
    {
        // Arrange
        Project project = await CreateAndSaveProjectAsync("project-scim-id");
        SCIMMember scimMember = CreateScimMember("New User", "new-user-scim-id");
        SCIMGroup scimGroup = CreateScimGroup(project.SCIMId, [scimMember]);
        _mockScimApiClient.Setup(m => m.GetSCIMGroup(project.SCIMId)).ReturnsAsync(scimGroup);

        // Act
        await _service.SyncProjectAsync(project.Id);

        // Assert
        Project updatedProject = await _context.Projects.Include(p => p.Users).SingleAsync();
        User newUser = Assert.Single(updatedProject.Users);
        Assert.Equal("new-user-scim-id", newUser.SCIMId);
    }

    [Fact]
    public async Task SyncProjectAsync_WithNonExistentProject_ThrowsProjectNotFoundException()
    {
        // Arrange
        Guid nonExistentProjectId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ProjectNotFoundException>(() => _service.SyncProjectAsync(nonExistentProjectId));
    }

    [Fact]
    public async Task SyncProjectAsync_WithProjectNotFoundInApi_ThrowsProjectNotFoundException()
    {
        // Arrange
        Project project = await CreateAndSaveProjectAsync("project-scim-id");
        _mockScimApiClient.Setup(m => m.GetSCIMGroup(project.SCIMId)).ReturnsAsync((SCIMGroup)null!);

        // Act & Assert
        await Assert.ThrowsAsync<ProjectNotFoundException>(() => _service.SyncProjectAsync(project.Id));
    }

    [Fact]
    public async Task SyncProjectAsync_WithValidProject_SyncsSuccessfully()
    {
        // Arrange
        (User existingUser, _) = await CreateAndSaveUserAsync("Existing User", "existing-user-scim-id");
        Project project = await CreateAndSaveProjectAsync("project-scim-id", [existingUser]);

        SCIMMember[] members = new[]
        {
            CreateScimMember("Existing User", "existing-user-scim-id"),
            CreateScimMember("New User", "new-user-scim-id")
        };
        SCIMGroup scimGroup = CreateScimGroup(project.SCIMId, members);
        _mockScimApiClient.Setup(m => m.GetSCIMGroup(project.SCIMId)).ReturnsAsync(scimGroup);

        // Act
        await _service.SyncProjectAsync(project.Id);

        // Assert
        Project updatedProject = await _context.Projects.Include(p => p.Users).SingleAsync();
        Assert.Equal(2, updatedProject.Users.Count);
        Assert.Contains(updatedProject.Users, u => u.SCIMId == "existing-user-scim-id");
        Assert.Contains(updatedProject.Users, u => u.SCIMId == "new-user-scim-id");
    }

    [Fact]
    public async Task SyncProjectRoleAsync_WithContributorRole_CreatesContributor()
    {
        // Arrange
        (User user, Person person) = await CreateAndSaveUserAsync("Test Contributor", "contributor-scim-id");
        Project project = await CreateAndSaveProjectAsync("project-scim-id", [user]);
        UserRole role = new()
        {
            Id = Guid.NewGuid(),
            Urn = "urn:mace:surf.nl:sram:group:conflux-contributor",
            ProjectId = project.Id,
            Type = UserRoleType.Contributor,
            SCIMId = "role-scim-id"
        };
        
        // Add the role to the project and save it to the database first
        user.Roles.Add(role);
        _context.UserRoles.Add(role);
        await _context.SaveChangesAsync();

        SCIMGroup scimGroup = CreateScimGroup(role.SCIMId, [CreateScimMember(person.Name, user.SCIMId)]);
        _mockScimApiClient.Setup(m => m.GetSCIMGroup(role.SCIMId)).ReturnsAsync(scimGroup);

        // Act
        await _service.SyncProjectRoleAsync(project, role);
        await _context.SaveChangesAsync(); // Service adds but doesn't save, so we save to assert DB state.

        // Assert
        Contributor contributor = await _context.Contributors.SingleAsync(c => c.ProjectId == project.Id);
        Assert.Equal(person.Id, contributor.PersonId);
    }

    [Fact]
    public async Task SyncProjectRoleAsync_RemovingContributorRole_EndsPositions()
    {
        // Arrange
        (User user, Person person) = await CreateAndSaveUserAsync("Test Contributor", "contributor-scim-id");
        Project project = await CreateAndSaveProjectAsync("project-scim-id", [user]);
        UserRole role = new()
        {
            Id = Guid.NewGuid(),
            Urn = "urn:mace:surf.nl:sram:group:conflux-contributor",
            ProjectId = project.Id,
            Type = UserRoleType.Contributor,
            SCIMId = "role-scim-id"
        };

        // Add the role to the project and save it to the database first
        user.Roles.Add(role);
        _context.UserRoles.Add(role);

        ContributorPosition position = new()
            { PersonId = person.Id, ProjectId = project.Id, Position = ContributorPositionType.Other, StartDate = DateTime.UtcNow, EndDate = null };
        Contributor contributor = new()
        { PersonId = person.Id, ProjectId = project.Id, Positions = [position]
        };
        project.Contributors.Add(contributor);
        await _context.SaveChangesAsync();

        SCIMGroup scimGroup = CreateScimGroup(role.SCIMId, []); // User removed
        _mockScimApiClient.Setup(m => m.GetSCIMGroup(role.SCIMId)).ReturnsAsync(scimGroup);

        // Act
        await _service.SyncProjectRoleAsync(project, role);

        // Assert
        ContributorPosition updatedPosition = await _context.ContributorPositions.SingleAsync();
        Assert.NotNull(updatedPosition.EndDate);
    }
}