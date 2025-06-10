// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
//
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
using Conflux.Domain.Logic.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Conflux.Domain.Logic.Tests.Services;

public class AccessControlServiceTests : IDisposable
{
    private readonly ConfluxContext _context;
    private readonly AccessControlService _service;

    public AccessControlServiceTests()
    {
        ServiceProvider serviceProvider = new ServiceCollection()
            .AddEntityFrameworkInMemoryDatabase()
            .BuildServiceProvider();

        DbContextOptions<ConfluxContext> options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.CreateVersion7()}") // Unique name for isolation
            .UseInternalServiceProvider(serviceProvider)
            .Options;

        _context = new(options);
        _context.Database.EnsureCreated();
        _service = new(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Helper Methods

    /// <summary>
    /// Creates a user with an associated person, saves them to the database, and returns the user entity.
    /// </summary>
    private async Task<User> CreateUserAsync(
        PermissionLevel permissionLevel = PermissionLevel.User,
        List<string>? lectorates = null,
        List<string>? organisations = null)
    {
        Person person = new()
            { Id = Guid.CreateVersion7(), Name = "Test User" };
        User user = new()
        {
            Id = Guid.CreateVersion7(),
            SCIMId = $"user-scim-id-{Guid.CreateVersion7()}",
            Person = person,
            PermissionLevel = permissionLevel,
            AssignedLectorates = lectorates ??
            [
            ],
            AssignedOrganisations = organisations ??
            [
            ],
            PersonId = person.Id
        };
        person.User = user;

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    /// <summary>
    /// Creates a project, saves it to the database, and returns the project entity.
    /// </summary>
    private async Task<Project> CreateProjectAsync(string? lectorate = null, string? ownerOrg = null)
    {
        Project project = new()
        {
            Id = Guid.CreateVersion7(),
            Lectorate = lectorate,
            OwnerOrganisation = ownerOrg
        };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();
        return project;
    }

    /// <summary>
    /// Creates a role and assigns it to a user for a specific project.
    /// </summary>
    private async Task CreateAndAssignRoleAsync(User user, Project project, UserRoleType roleType)
    {
        UserRole userRole = new()
        {
            Id = Guid.CreateVersion7(),
            ProjectId = project.Id,
            Type = roleType,
            Urn = "test-urn",
            SCIMId = "test-scim-id"
        };
        
        // Add the UserRole to the context first
        _context.UserRoles.Add(userRole);
        await _context.SaveChangesAsync();
        
        // Now add the relationship
        user.Roles.Add(userRole);
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    #endregion

    [Fact]
    public async Task UserHasRoleInProject_UserWithRoleExists_ReturnsTrue()
    {
        // Arrange
        Project project = await CreateProjectAsync();
        User user = await CreateUserAsync();
        await CreateAndAssignRoleAsync(user, project, UserRoleType.Admin);

        // Act
        bool result = await _service.UserHasRoleInProject(user.Id, project.Id, UserRoleType.Admin);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task UserHasRoleInProject_UserWithDifferentRoleExists_ReturnsFalse()
    {
        // Arrange
        Project project = await CreateProjectAsync();
        User user = await CreateUserAsync();
        await CreateAndAssignRoleAsync(user, project, UserRoleType.User);

        // Act
        bool result = await _service.UserHasRoleInProject(user.Id, project.Id, UserRoleType.Admin);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UserHasRoleInProject_UserWithRoleInDifferentProject_ReturnsFalse()
    {
        // Arrange
        Project project = await CreateProjectAsync();
        Project differentProject = await CreateProjectAsync();
        User user = await CreateUserAsync();
        await CreateAndAssignRoleAsync(user, differentProject, UserRoleType.Admin);

        // Act
        bool result = await _service.UserHasRoleInProject(user.Id, project.Id, UserRoleType.Admin);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UserHasRoleInProject_UserWithNoRoles_ReturnsFalse()
    {
        // Arrange
        Project project = await CreateProjectAsync();
        User user = await CreateUserAsync();

        // Act
        bool result = await _service.UserHasRoleInProject(user.Id, project.Id, UserRoleType.Admin);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UserHasRoleInProject_UserDoesNotExist_ReturnsFalse()
    {
        // Arrange
        Project project = await CreateProjectAsync();
        Guid nonExistentUserId = Guid.CreateVersion7();

        // Act
        bool result = await _service.UserHasRoleInProject(nonExistentUserId, project.Id, UserRoleType.Admin);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UserHasRoleInProject_ProjectDoesNotExist_ReturnsFalse()
    {
        // Arrange
        User user = await CreateUserAsync();
        Guid nonExistentProjectId = Guid.CreateVersion7();

        // Act
        bool result = await _service.UserHasRoleInProject(user.Id, nonExistentProjectId, UserRoleType.Admin);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UserHasRoleInProject_SuperAdminUser_AlwaysReturnsTrue()
    {
        // Arrange
        Project project = await CreateProjectAsync();
        User user = await CreateUserAsync(permissionLevel: PermissionLevel.SuperAdmin);

        // Act
        bool result = await _service.UserHasRoleInProject(user.Id, project.Id, UserRoleType.Admin);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task UserHasRoleInProject_SystemAdminWithMatchingLectorate_ReturnsTrue()
    {
        // Arrange
        Project project = await CreateProjectAsync(lectorate: "Computer Science");
        User user = await CreateUserAsync(
            permissionLevel: PermissionLevel.SystemAdmin,
            lectorates: ["Computer Science", "Mathematics"]
        );

        // Act
        bool result = await _service.UserHasRoleInProject(user.Id, project.Id, UserRoleType.Admin);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task UserHasRoleInProject_SystemAdminWithMatchingOrganisation_ReturnsTrue()
    {
        // Arrange
        Project project = await CreateProjectAsync(ownerOrg: "Utrecht University");
        User user = await CreateUserAsync(
            permissionLevel: PermissionLevel.SystemAdmin,
            organisations: ["Utrecht University", "Another University"]
        );

        // Act
        bool result = await _service.UserHasRoleInProject(user.Id, project.Id, UserRoleType.User);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task UserHasRoleInProject_SystemAdminWithoutMatch_FallsBackToRole_ReturnsTrue()
    {
        // Arrange
        Project project = await CreateProjectAsync(lectorate: "Physics", ownerOrg: "Different University");
        User user = await CreateUserAsync(
            permissionLevel: PermissionLevel.SystemAdmin,
            lectorates: ["Mathematics"],
            organisations: ["Utrecht University"]
        );
        await CreateAndAssignRoleAsync(user, project, UserRoleType.Admin);

        // Act
        bool result = await _service.UserHasRoleInProject(user.Id, project.Id, UserRoleType.Admin);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task UserHasRoleInProject_SystemAdminWithoutMatchOrRole_ReturnsFalse()
    {
        // Arrange
        Project project = await CreateProjectAsync(lectorate: "Physics", ownerOrg: "Different University");
        User user = await CreateUserAsync(
            permissionLevel: PermissionLevel.SystemAdmin,
            lectorates: ["Mathematics"],
            organisations: ["Utrecht University"]
        );

        // Act
        bool result = await _service.UserHasRoleInProject(user.Id, project.Id, UserRoleType.Admin);

        // Assert
        Assert.False(result);
    }
}