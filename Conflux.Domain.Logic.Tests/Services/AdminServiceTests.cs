// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
//
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
using Conflux.Domain.Logic.DTOs.Responses;
using Conflux.Domain.Logic.Exceptions;
using Conflux.Domain.Logic.Services;
using Conflux.Domain.Session;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Conflux.Domain.Logic.Tests.Services;

public class AdminServiceTests : IDisposable
{
    private readonly ConfluxContext _context;
    private readonly AdminService _service;
    private readonly Mock<IUserSessionService> _mockUserSessionService;
    private readonly Mock<IConfiguration> _mockConfiguration;

    public AdminServiceTests()
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

        _mockUserSessionService = new();
        _mockConfiguration = new();
        _service = new(_context, _mockConfiguration.Object, _mockUserSessionService.Object);
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
    private async Task<User> CreateAndSaveUserAsync(string name, string email, PermissionLevel permissionLevel = PermissionLevel.User)
    {
        Guid userId = Guid.CreateVersion7();
        Person person = new()
        {
            Id = Guid.CreateVersion7(),
            Name = name,
            Email = email,
            UserId = userId
        };
        User user = new()
        {
            Id = userId,
            SCIMId = $"scim-id-{Guid.CreateVersion7()}",
            Person = person,
            PermissionLevel = permissionLevel,
            PersonId = person.Id,
        };
        person.User = user;

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    /// <summary>
    /// Creates a project, saves it to the database, and returns the project entity.
    /// </summary>
    private async Task<Project> CreateAndSaveProjectAsync(string? ownerOrganisation = null)
    {
        Project project = new()
        {
            Id = Guid.CreateVersion7(),
            OwnerOrganisation = ownerOrganisation
        };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();
        return project;
    }

    /// <summary>
    /// Configures the mock IUserSessionService to return a specific user session.
    /// </summary>
    private void SetupUserSession(User user)
    {
        UserSession userSession = new()
        {
            Email = user.Person!.Email!,
            Name = user.Person.Name,
            User = user
        };
        _mockUserSessionService.Setup(s => s.GetUser()).ReturnsAsync(userSession);
    }

    #endregion

    [Fact]
    public async Task GetUsersByQuery_WithSuperAdminUser_ReturnsAllUsers()
    {
        // Arrange
        User superAdmin = await CreateAndSaveUserAsync("Super Admin", "admin@test.com", PermissionLevel.SuperAdmin);
        await CreateAndSaveUserAsync("Regular User", "user@test.com");
        await CreateAndSaveUserAsync("System Admin", "sysadmin@test.com", PermissionLevel.SystemAdmin);
        SetupUserSession(superAdmin);

        // Act
        List<UserResponseDTO> result = await _service.GetUsersByQuery(null, false);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains(result, u => u.Person!.Name == "Super Admin");
        Assert.Contains(result, u => u.Person!.Name == "Regular User");
        Assert.Contains(result, u => u.Person!.Name == "System Admin");
    }

    [Fact]
    public async Task GetUsersByQuery_WithQuery_ReturnsFilteredUsers()
    {
        // Arrange
        User superAdmin = await CreateAndSaveUserAsync("Super Admin", "admin@test.com", PermissionLevel.SuperAdmin);
        await CreateAndSaveUserAsync("John Doe", "john@test.com");
        await CreateAndSaveUserAsync("Jane Smith", "jane@test.com");
        SetupUserSession(superAdmin);

        // Act
        List<UserResponseDTO> result = await _service.GetUsersByQuery("John", false);

        // Assert
        Assert.Single(result);
        Assert.Equal("John Doe", result[0].Person!.Name);
    }

    [Fact]
    public async Task GetUsersByQuery_WithAdminsOnly_ReturnsOnlyAdmins()
    {
        // Arrange
        User superAdmin = await CreateAndSaveUserAsync("Super Admin", "admin@test.com", PermissionLevel.SuperAdmin);
        await CreateAndSaveUserAsync("Regular User", "user@test.com");
        await CreateAndSaveUserAsync("System Admin", "sysadmin@test.com", PermissionLevel.SystemAdmin);
        SetupUserSession(superAdmin);

        // Act
        List<UserResponseDTO> result = await _service.GetUsersByQuery(null, true);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, u => u.PermissionLevel == PermissionLevel.SuperAdmin);
        Assert.Contains(result, u => u.PermissionLevel == PermissionLevel.SystemAdmin);
    }

    [Fact]
    public async Task GetUsersByQuery_WithNonSuperAdminUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        User regularUser = await CreateAndSaveUserAsync("Regular User", "user@test.com");
        SetupUserSession(regularUser);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.GetUsersByQuery(null, false));
    }

    [Fact]
    public async Task SetUserPermissionLevel_WithValidUser_UpdatesPermissionLevel()
    {
        // Arrange
        User superAdmin = await CreateAndSaveUserAsync("Super Admin", "admin@test.com", PermissionLevel.SuperAdmin);
        User targetUser = await CreateAndSaveUserAsync("Target User", "target@test.com");
        SetupUserSession(superAdmin);

        // Act
        UserResponseDTO result = await _service.SetUserPermissionLevel(targetUser.Id, PermissionLevel.SystemAdmin);

        // Assert
        Assert.Equal(PermissionLevel.SystemAdmin, result.PermissionLevel);
        User? updatedUserInDb = await _context.Users.FindAsync(targetUser.Id);
        Assert.Equal(PermissionLevel.SystemAdmin, updatedUserInDb!.PermissionLevel);
    }

    [Fact]
    public async Task SetPermissionLevel_WithSuperAdminLevel_ThrowsArgumentException()
    {
        // Arrange
        User superAdmin = await CreateAndSaveUserAsync("Super Admin", "admin@test.com", PermissionLevel.SuperAdmin);
        User targetUser = await CreateAndSaveUserAsync("Target User", "target@test.com");
        SetupUserSession(superAdmin);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SetUserPermissionLevel(targetUser.Id, PermissionLevel.SuperAdmin));
    }

    [Fact]
    public async Task GetAvailableLectorates_ReturnsConfiguredLectorates()
    {
        // Arrange
        List<string> lectorates = ["Computer Science", "Data Science"];
        List<IConfigurationSection> configSections = lectorates.Select(l =>
        {
            Mock<IConfigurationSection> mockSection = new();
            mockSection.Setup(s => s.Value).Returns(l);
            return mockSection.Object;
        }).ToList();

        _mockConfiguration.Setup(c => c.GetSection("Lectorates").GetChildren()).Returns(configSections);

        // Act
        List<string> result = await _service.GetAvailableLectorates();

        // Assert
        Assert.Equal(lectorates, result);
    }

    [Fact]
    public async Task GetAvailableOrganisations_WithSuperAdminUser_ReturnsDistinctOrganisations()
    {
        // Arrange
        User superAdmin = await CreateAndSaveUserAsync("Super Admin", "admin@test.com", PermissionLevel.SuperAdmin);
        SetupUserSession(superAdmin);

        await CreateAndSaveProjectAsync("Organization A");
        await CreateAndSaveProjectAsync("Organization B");
        await CreateAndSaveProjectAsync("Organization A"); // Duplicate
        await CreateAndSaveProjectAsync(null); // Null
        await CreateAndSaveProjectAsync(""); // Empty

        // Act
        List<string> result = await _service.GetAvailableOrganisations();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("Organization A", result);
        Assert.Contains("Organization B", result);
    }

    [Fact]
    public async Task AssignLectoratesToUser_WithValidUser_AssignsLectoratesSuccessfully()
    {
        // Arrange
        User superAdmin = await CreateAndSaveUserAsync("Super Admin", "admin@test.com", PermissionLevel.SuperAdmin);
        User targetUser = await CreateAndSaveUserAsync("Target User", "target@test.com");
        SetupUserSession(superAdmin);
        List<string> lectorates = ["Computer Science", "Data Science"];

        // Act
        UserResponseDTO result = await _service.AssignLectoratesToUser(targetUser.Id, lectorates);

        // Assert
        Assert.Equal(lectorates, result.AssignedLectorates);
        User? updatedUserInDb = await _context.Users.FindAsync(targetUser.Id);
        Assert.Equal(lectorates, updatedUserInDb!.AssignedLectorates);
    }

    [Fact]
    public async Task AssignOrganisationsToUser_WithValidUser_AssignsOrganisationsSuccessfully()
    {
        // Arrange
        User superAdmin = await CreateAndSaveUserAsync("Super Admin", "admin@test.com", PermissionLevel.SuperAdmin);
        User targetUser = await CreateAndSaveUserAsync("Target User", "target@test.com");
        SetupUserSession(superAdmin);
        List<string> organisations = ["Organization A", "Organization B"];

        // Act
        UserResponseDTO result = await _service.AssignOrganisationsToUser(targetUser.Id, organisations);

        // Assert
        Assert.Equal(organisations, result.AssignedOrganisations);
        User? updatedUserInDb = await _context.Users.FindAsync(targetUser.Id);
        Assert.Equal(organisations, updatedUserInDb!.AssignedOrganisations);
    }
}