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

public class AdminServiceTests
{
    private ConfluxContext CreateContext(string dbName)
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

    private static User CreateUserWithPerson(Guid userId, string name, string email, PermissionLevel permissionLevel = PermissionLevel.User, string scimId = "test-scim")
    {
        var personId = Guid.NewGuid();
        
        var person = new Person
        {
            Id = personId,
            Name = name,
            Email = email,
            GivenName = name.Split(' ').FirstOrDefault(),
            FamilyName = name.Split(' ').LastOrDefault(),
            ORCiD = null,
            UserId = userId
        };
        
        var user = new User
        {
            Id = userId,
            SCIMId = scimId,
            PersonId = personId,
            Person = person,
            PermissionLevel = permissionLevel,
            AssignedLectorates = [],
            AssignedOrganisations = [],
            Roles = []
        };
        
        person.User = user;
        return user;
    }

    private static Project CreateProject(string? ownerOrganisation = null)
    {
        return new Project
        {
            Id = Guid.NewGuid(),
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(6),
            OwnerOrganisation = ownerOrganisation,
            Titles = [],
            Descriptions = [],
            Contributors = [],
            Products = [],
            Organisations = [],
            Users = []
        };
    }

    private static UserSession CreateSuperAdminSession(User user)
    {
        return new UserSession
        {
            Email = user.Person!.Email!,
            Name = user.Person.Name,
            SRAMId = "sram-id",
            User = user,
            Collaborations = []
        };
    }

    private static UserSession CreateRegularUserSession(User user)
    {
        return new UserSession
        {
            Email = user.Person!.Email!,
            Name = user.Person.Name,
            SRAMId = "sram-id",
            User = user,
            Collaborations = []
        };
    }

    [Fact]
    public async Task GetUsersByQuery_WithSuperAdminUser_ReturnsAllUsers()
    {
        // Arrange
        string dbName = $"GetUsersByQuery_AllUsers_{Guid.NewGuid()}";
        ConfluxContext context = CreateContext(dbName);

        var superAdminUser = CreateUserWithPerson(Guid.NewGuid(), "Super Admin", "admin@test.com", PermissionLevel.SuperAdmin);
        var regularUser = CreateUserWithPerson(Guid.NewGuid(), "Regular User", "user@test.com", PermissionLevel.User);
        var systemAdminUser = CreateUserWithPerson(Guid.NewGuid(), "System Admin", "sysadmin@test.com", PermissionLevel.SystemAdmin);

        context.People.AddRange(superAdminUser.Person!, regularUser.Person!, systemAdminUser.Person!);
        context.Users.AddRange(superAdminUser, regularUser, systemAdminUser);
        await context.SaveChangesAsync();

        var mockUserSessionService = new Mock<IUserSessionService>();
        mockUserSessionService.Setup(s => s.GetUser()).ReturnsAsync(CreateSuperAdminSession(superAdminUser));

        var mockConfiguration = new Mock<IConfiguration>();
        
        AdminService service = new(context, mockConfiguration.Object, mockUserSessionService.Object);

        // Act
        List<UserResponseDTO> result = await service.GetUsersByQuery(null, false);

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
        string dbName = $"GetUsersByQuery_WithQuery_{Guid.NewGuid()}";
        ConfluxContext context = CreateContext(dbName);

        var superAdminUser = CreateUserWithPerson(Guid.NewGuid(), "Super Admin", "admin@test.com", PermissionLevel.SuperAdmin);
        var johnUser = CreateUserWithPerson(Guid.NewGuid(), "John Doe", "john@test.com", PermissionLevel.User);
        var janeUser = CreateUserWithPerson(Guid.NewGuid(), "Jane Smith", "jane@test.com", PermissionLevel.User);

        context.People.AddRange(superAdminUser.Person!, johnUser.Person!, janeUser.Person!);
        context.Users.AddRange(superAdminUser, johnUser, janeUser);
        await context.SaveChangesAsync();

        var mockUserSessionService = new Mock<IUserSessionService>();
        mockUserSessionService.Setup(s => s.GetUser()).ReturnsAsync(CreateSuperAdminSession(superAdminUser));

        var mockConfiguration = new Mock<IConfiguration>();
        
        AdminService service = new(context, mockConfiguration.Object, mockUserSessionService.Object);

        // Act
        List<UserResponseDTO> result = await service.GetUsersByQuery("John", false);

        // Assert
        Assert.Single(result);
        Assert.Equal("John Doe", result[0].Person!.Name);
    }

    [Fact]
    public async Task GetUsersByQuery_WithAdminsOnly_ReturnsOnlyAdmins()
    {
        // Arrange
        string dbName = $"GetUsersByQuery_AdminsOnly_{Guid.NewGuid()}";
        ConfluxContext context = CreateContext(dbName);

        var superAdminUser = CreateUserWithPerson(Guid.NewGuid(), "Super Admin", "admin@test.com", PermissionLevel.SuperAdmin);
        var regularUser = CreateUserWithPerson(Guid.NewGuid(), "Regular User", "user@test.com", PermissionLevel.User);
        var systemAdminUser = CreateUserWithPerson(Guid.NewGuid(), "System Admin", "sysadmin@test.com", PermissionLevel.SystemAdmin);

        context.People.AddRange(superAdminUser.Person!, regularUser.Person!, systemAdminUser.Person!);
        context.Users.AddRange(superAdminUser, regularUser, systemAdminUser);
        await context.SaveChangesAsync();

        var mockUserSessionService = new Mock<IUserSessionService>();
        mockUserSessionService.Setup(s => s.GetUser()).ReturnsAsync(CreateSuperAdminSession(superAdminUser));

        var mockConfiguration = new Mock<IConfiguration>();
        
        AdminService service = new(context, mockConfiguration.Object, mockUserSessionService.Object);

        // Act
        List<UserResponseDTO> result = await service.GetUsersByQuery(null, true);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, u => u.Person!.Name == "Super Admin");
        Assert.Contains(result, u => u.Person!.Name == "System Admin");
        Assert.DoesNotContain(result, u => u.Person!.Name == "Regular User");
    }

    [Fact]
    public async Task GetUsersByQuery_WithNonSuperAdminUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        string dbName = $"GetUsersByQuery_Unauthorized_{Guid.NewGuid()}";
        ConfluxContext context = CreateContext(dbName);

        var regularUser = CreateUserWithPerson(Guid.NewGuid(), "Regular User", "user@test.com", PermissionLevel.User);
        context.People.Add(regularUser.Person!);
        context.Users.Add(regularUser);
        await context.SaveChangesAsync();

        var mockUserSessionService = new Mock<IUserSessionService>();
        mockUserSessionService.Setup(s => s.GetUser()).ReturnsAsync(CreateRegularUserSession(regularUser));

        var mockConfiguration = new Mock<IConfiguration>();
        
        AdminService service = new(context, mockConfiguration.Object, mockUserSessionService.Object);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetUsersByQuery(null, false));
    }

    [Fact]
    public async Task GetUsersByQuery_WithNullUserSession_ThrowsUserNotAuthenticatedException()
    {
        // Arrange
        string dbName = $"GetUsersByQuery_NullSession_{Guid.NewGuid()}";
        ConfluxContext context = CreateContext(dbName);

        var mockUserSessionService = new Mock<IUserSessionService>();
        mockUserSessionService.Setup(s => s.GetUser()).ReturnsAsync((UserSession?)null);

        var mockConfiguration = new Mock<IConfiguration>();
        
        AdminService service = new(context, mockConfiguration.Object, mockUserSessionService.Object);

        // Act & Assert
        await Assert.ThrowsAsync<UserNotAuthenticatedException>(() => service.GetUsersByQuery(null, false));
    }


    [Fact]
    public async Task SetUserPermissionLevel_WithValidUser_UpdatesPermissionLevelSuccessfully()
    {
        // Arrange
        string dbName = $"SetUserPermission_ValidUser_{Guid.NewGuid()}";
        ConfluxContext context = CreateContext(dbName);

        var superAdminUser = CreateUserWithPerson(Guid.NewGuid(), "Super Admin", "admin@test.com", PermissionLevel.SuperAdmin);
        var targetUserId = Guid.NewGuid();
        var targetUser = CreateUserWithPerson(targetUserId, "Target User", "target@test.com", PermissionLevel.User);

        context.People.AddRange(superAdminUser.Person!, targetUser.Person!);
        context.Users.AddRange(superAdminUser, targetUser);
        await context.SaveChangesAsync();

        var mockUserSessionService = new Mock<IUserSessionService>();
        mockUserSessionService.Setup(s => s.GetUser()).ReturnsAsync(CreateSuperAdminSession(superAdminUser));

        var mockConfiguration = new Mock<IConfiguration>();
        
        AdminService service = new(context, mockConfiguration.Object, mockUserSessionService.Object);

        // Act
        UserResponseDTO result = await service.SetUserPermissionLevel(targetUserId, PermissionLevel.SystemAdmin);

        // Assert
        Assert.Equal(PermissionLevel.SystemAdmin, result.PermissionLevel);
        
        // Verify database was updated
        User? updatedUser = await context.Users.FindAsync(targetUserId);
        Assert.NotNull(updatedUser);
        Assert.Equal(PermissionLevel.SystemAdmin, updatedUser.PermissionLevel);
    }

    [Fact]
    public async Task SetPermissionLevel_WithSuperAdminLevel_ThrowsArgumentException()
    {
        // Arrange
        string dbName = $"SetPermissionLevel_SuperAdmin_{Guid.NewGuid()}";
        ConfluxContext context = CreateContext(dbName);

        var superAdminUser = CreateUserWithPerson(Guid.NewGuid(), "Super Admin", "admin@test.com", PermissionLevel.SuperAdmin);
        var targetUserId = Guid.NewGuid();
        var targetUser = CreateUserWithPerson(targetUserId, "Target User", "target@test.com", PermissionLevel.User);

        context.People.AddRange(superAdminUser.Person!, targetUser.Person!);
        context.Users.AddRange(superAdminUser, targetUser);
        await context.SaveChangesAsync();

        var mockUserSessionService = new Mock<IUserSessionService>();
        mockUserSessionService.Setup(s => s.GetUser()).ReturnsAsync(CreateSuperAdminSession(superAdminUser));

        var mockConfiguration = new Mock<IConfiguration>();
        
        AdminService service = new(context, mockConfiguration.Object, mockUserSessionService.Object);

        // Act & Assert
        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            service.SetUserPermissionLevel(targetUserId, PermissionLevel.SuperAdmin));
        Assert.Contains("Cannot set user permission level to SuperAdmin", exception.Message);
    }

    [Fact]
    public async Task SetPermissionLevel_WithNonExistentUser_ThrowsException()
    {
        // Arrange
        string dbName = $"SetPermissionLevel_NonExistent_{Guid.NewGuid()}";
        ConfluxContext context = CreateContext(dbName);

        var superAdminUser = CreateUserWithPerson(Guid.NewGuid(), "Super Admin", "admin@test.com", PermissionLevel.SuperAdmin);
        context.People.Add(superAdminUser.Person!);
        context.Users.Add(superAdminUser);
        await context.SaveChangesAsync();

        var mockUserSessionService = new Mock<IUserSessionService>();
        mockUserSessionService.Setup(s => s.GetUser()).ReturnsAsync(CreateSuperAdminSession(superAdminUser));

        var mockConfiguration = new Mock<IConfiguration>();
        
        AdminService service = new(context, mockConfiguration.Object, mockUserSessionService.Object);

        var nonExistentUserId = Guid.NewGuid();

        // Act & Assert
        Exception exception = await Assert.ThrowsAsync<Exception>(() => 
            service.SetUserPermissionLevel(nonExistentUserId, PermissionLevel.SystemAdmin));
        Assert.Contains($"User with ID {nonExistentUserId} not found", exception.Message);
    }

    [Fact]
    public async Task SetPermissionLevel_WithNonSuperAdminUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        string dbName = $"SetPermissionLevel_Unauthorized_{Guid.NewGuid()}";
        ConfluxContext context = CreateContext(dbName);

        var regularUser = CreateUserWithPerson(Guid.NewGuid(), "Regular User", "user@test.com", PermissionLevel.User);
        context.People.Add(regularUser.Person!);
        context.Users.Add(regularUser);
        await context.SaveChangesAsync();

        var mockUserSessionService = new Mock<IUserSessionService>();
        mockUserSessionService.Setup(s => s.GetUser()).ReturnsAsync(CreateRegularUserSession(regularUser));

        var mockConfiguration = new Mock<IConfiguration>();
        
        AdminService service = new(context, mockConfiguration.Object, mockUserSessionService.Object);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
            service.SetUserPermissionLevel(Guid.NewGuid(), PermissionLevel.SystemAdmin));
    }

    [Fact]
    public async Task GetAvailableLectorates_ReturnsConfiguredLectorates()
    {
        // Arrange
        string dbName = $"GetAvailableLectorates_{Guid.NewGuid()}";
        ConfluxContext context = CreateContext(dbName);

        var mockUserSessionService = new Mock<IUserSessionService>();
        
        var mockConfiguration = new Mock<IConfiguration>();
        var mockSection = new Mock<IConfigurationSection>();
        
        // Mock the section children to return our lectorates
        var lectorateChildren = new List<IConfigurationSection>();
        var lectorates = new List<string> { "Computer Science", "Information Systems", "Data Science" };
        
        for (int i = 0; i < lectorates.Count; i++)
        {
            var mockChild = new Mock<IConfigurationSection>();
            mockChild.Setup(c => c.Value).Returns(lectorates[i]);
            lectorateChildren.Add(mockChild.Object);
        }
        
        mockSection.Setup(s => s.GetChildren()).Returns(lectorateChildren);
        mockConfiguration.Setup(c => c.GetSection("Lectorates")).Returns(mockSection.Object);
        
        AdminService service = new(context, mockConfiguration.Object, mockUserSessionService.Object);

        // Act
        List<string> result = await service.GetAvailableLectorates();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains("Computer Science", result);
        Assert.Contains("Information Systems", result);
        Assert.Contains("Data Science", result);
    }

    [Fact]
    public async Task GetAvailableLectorates_WithNullConfiguration_ReturnsEmptyList()
    {
        // Arrange
        string dbName = $"GetAvailableLectorates_Null_{Guid.NewGuid()}";
        ConfluxContext context = CreateContext(dbName);

        var mockUserSessionService = new Mock<IUserSessionService>();
        
        var mockConfiguration = new Mock<IConfiguration>();
        var mockSection = new Mock<IConfigurationSection>();
        
        // Mock empty children to simulate null/empty configuration
        mockSection.Setup(s => s.GetChildren()).Returns(new List<IConfigurationSection>());
        mockConfiguration.Setup(c => c.GetSection("Lectorates")).Returns(mockSection.Object);
        
        AdminService service = new(context, mockConfiguration.Object, mockUserSessionService.Object);

        // Act
        List<string> result = await service.GetAvailableLectorates();

        // Assert
        Assert.Empty(result);
    }


    [Fact]
    public async Task GetAvailableOrganisations_WithSuperAdminUser_ReturnsDistinctOrganisations()
    {
        // Arrange
        string dbName = $"GetAvailableOrganisations_Valid_{Guid.NewGuid()}";
        ConfluxContext context = CreateContext(dbName);

        var superAdminUser = CreateUserWithPerson(Guid.NewGuid(), "Super Admin", "admin@test.com", PermissionLevel.SuperAdmin);
        context.People.Add(superAdminUser.Person!);
        context.Users.Add(superAdminUser);

        // Add projects with different organizations
        var project1 = CreateProject("Organization A");
        var project2 = CreateProject("Organization B");
        var project3 = CreateProject("Organization A"); // Duplicate
        var project4 = CreateProject(null); // Null organization
        var project5 = CreateProject(""); // Empty organization

        context.Projects.AddRange(project1, project2, project3, project4, project5);
        await context.SaveChangesAsync();

        var mockUserSessionService = new Mock<IUserSessionService>();
        mockUserSessionService.Setup(s => s.GetUser()).ReturnsAsync(CreateSuperAdminSession(superAdminUser));

        var mockConfiguration = new Mock<IConfiguration>();
        
        AdminService service = new(context, mockConfiguration.Object, mockUserSessionService.Object);

        // Act
        List<string> result = await service.GetAvailableOrganisations();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("Organization A", result);
        Assert.Contains("Organization B", result);
    }

    [Fact]
    public async Task GetAvailableOrganisations_WithNonSuperAdminUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        string dbName = $"GetAvailableOrganisations_Unauthorized_{Guid.NewGuid()}";
        ConfluxContext context = CreateContext(dbName);

        var regularUser = CreateUserWithPerson(Guid.NewGuid(), "Regular User", "user@test.com", PermissionLevel.User);
        context.People.Add(regularUser.Person!);
        context.Users.Add(regularUser);
        await context.SaveChangesAsync();

        var mockUserSessionService = new Mock<IUserSessionService>();
        mockUserSessionService.Setup(s => s.GetUser()).ReturnsAsync(CreateRegularUserSession(regularUser));

        var mockConfiguration = new Mock<IConfiguration>();
        
        AdminService service = new(context, mockConfiguration.Object, mockUserSessionService.Object);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetAvailableOrganisations());
    }

    [Fact]
    public async Task GetAvailableOrganisations_WithNullUserSession_ThrowsUserNotAuthenticatedException()
    {
        // Arrange
        string dbName = $"GetAvailableOrganisations_NullSession_{Guid.NewGuid()}";
        ConfluxContext context = CreateContext(dbName);

        var mockUserSessionService = new Mock<IUserSessionService>();
        mockUserSessionService.Setup(s => s.GetUser()).ReturnsAsync((UserSession?)null);

        var mockConfiguration = new Mock<IConfiguration>();
        
        AdminService service = new(context, mockConfiguration.Object, mockUserSessionService.Object);

        // Act & Assert
        await Assert.ThrowsAsync<UserNotAuthenticatedException>(() => service.GetAvailableOrganisations());
    }

    [Fact]
    public async Task AssignLectoratesToUser_WithValidUser_AssignsLectoratesSuccessfully()
    {
        // Arrange
        string dbName = $"AssignLectoratesToUser_Valid_{Guid.NewGuid()}";
        ConfluxContext context = CreateContext(dbName);

        var superAdminUser = CreateUserWithPerson(Guid.NewGuid(), "Super Admin", "admin@test.com", PermissionLevel.SuperAdmin);
        var targetUserId = Guid.NewGuid();
        var targetUser = CreateUserWithPerson(targetUserId, "Target User", "target@test.com", PermissionLevel.User);

        context.People.AddRange(superAdminUser.Person!, targetUser.Person!);
        context.Users.AddRange(superAdminUser, targetUser);
        await context.SaveChangesAsync();

        var mockUserSessionService = new Mock<IUserSessionService>();
        mockUserSessionService.Setup(s => s.GetUser()).ReturnsAsync(CreateSuperAdminSession(superAdminUser));

        var mockConfiguration = new Mock<IConfiguration>();
        
        AdminService service = new(context, mockConfiguration.Object, mockUserSessionService.Object);

        var lectorates = new List<string> { "Computer Science", "Data Science" };

        // Act
        UserResponseDTO result = await service.AssignLectoratesToUser(targetUserId, lectorates);

        // Assert
        Assert.Equal(2, result.AssignedLectorates.Count);
        Assert.Contains("Computer Science", result.AssignedLectorates);
        Assert.Contains("Data Science", result.AssignedLectorates);
        
        // Verify database was updated
        User? updatedUser = await context.Users.FindAsync(targetUserId);
        Assert.NotNull(updatedUser);
        Assert.Equal(2, updatedUser.AssignedLectorates.Count);
    }

    [Fact]
    public async Task AssignLectoratesToUser_WithNonExistentUser_ThrowsException()
    {
        // Arrange
        string dbName = $"AssignLectoratesToUser_NonExistent_{Guid.NewGuid()}";
        ConfluxContext context = CreateContext(dbName);

        var superAdminUser = CreateUserWithPerson(Guid.NewGuid(), "Super Admin", "admin@test.com", PermissionLevel.SuperAdmin);
        context.People.Add(superAdminUser.Person!);
        context.Users.Add(superAdminUser);
        await context.SaveChangesAsync();

        var mockUserSessionService = new Mock<IUserSessionService>();
        mockUserSessionService.Setup(s => s.GetUser()).ReturnsAsync(CreateSuperAdminSession(superAdminUser));

        var mockConfiguration = new Mock<IConfiguration>();
        
        AdminService service = new(context, mockConfiguration.Object, mockUserSessionService.Object);

        var nonExistentUserId = Guid.NewGuid();
        var lectorates = new List<string> { "Computer Science" };

        // Act & Assert
        Exception exception = await Assert.ThrowsAsync<Exception>(() => 
            service.AssignLectoratesToUser(nonExistentUserId, lectorates));
        Assert.Contains($"User with ID {nonExistentUserId} not found", exception.Message);
    }

    [Fact]
    public async Task AssignLectoratesToUser_WithNonSuperAdminUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        string dbName = $"AssignLectoratesToUser_Unauthorized_{Guid.NewGuid()}";
        ConfluxContext context = CreateContext(dbName);

        var regularUser = CreateUserWithPerson(Guid.NewGuid(), "Regular User", "user@test.com", PermissionLevel.User);
        context.People.Add(regularUser.Person!);
        context.Users.Add(regularUser);
        await context.SaveChangesAsync();

        var mockUserSessionService = new Mock<IUserSessionService>();
        mockUserSessionService.Setup(s => s.GetUser()).ReturnsAsync(CreateRegularUserSession(regularUser));

        var mockConfiguration = new Mock<IConfiguration>();
        
        AdminService service = new(context, mockConfiguration.Object, mockUserSessionService.Object);

        var lectorates = new List<string> { "Computer Science" };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
            service.AssignLectoratesToUser(Guid.NewGuid(), lectorates));
    }

    [Fact]
    public async Task AssignOrganisationsToUser_WithValidUser_AssignsOrganisationsSuccessfully()
    {
        // Arrange
        string dbName = $"AssignOrganisationsToUser_Valid_{Guid.NewGuid()}";
        ConfluxContext context = CreateContext(dbName);

        var superAdminUser = CreateUserWithPerson(Guid.NewGuid(), "Super Admin", "admin@test.com", PermissionLevel.SuperAdmin);
        var targetUserId = Guid.NewGuid();
        var targetUser = CreateUserWithPerson(targetUserId, "Target User", "target@test.com", PermissionLevel.User);

        context.People.AddRange(superAdminUser.Person!, targetUser.Person!);
        context.Users.AddRange(superAdminUser, targetUser);
        await context.SaveChangesAsync();

        var mockUserSessionService = new Mock<IUserSessionService>();
        mockUserSessionService.Setup(s => s.GetUser()).ReturnsAsync(CreateSuperAdminSession(superAdminUser));

        var mockConfiguration = new Mock<IConfiguration>();
        
        AdminService service = new(context, mockConfiguration.Object, mockUserSessionService.Object);

        var organisations = new List<string> { "Organization A", "Organization B" };

        // Act
        UserResponseDTO result = await service.AssignOrganisationsToUser(targetUserId, organisations);

        // Assert
        Assert.Equal(2, result.AssignedOrganisations.Count);
        Assert.Contains("Organization A", result.AssignedOrganisations);
        Assert.Contains("Organization B", result.AssignedOrganisations);
        
        // Verify database was updated
        User? updatedUser = await context.Users.FindAsync(targetUserId);
        Assert.NotNull(updatedUser);
        Assert.Equal(2, updatedUser.AssignedOrganisations.Count);
    }

    [Fact]
    public async Task AssignOrganisationsToUser_WithNonExistentUser_ThrowsException()
    {
        // Arrange
        string dbName = $"AssignOrganisationsToUser_NonExistent_{Guid.NewGuid()}";
        ConfluxContext context = CreateContext(dbName);

        var superAdminUser = CreateUserWithPerson(Guid.NewGuid(), "Super Admin", "admin@test.com", PermissionLevel.SuperAdmin);
        context.People.Add(superAdminUser.Person!);
        context.Users.Add(superAdminUser);
        await context.SaveChangesAsync();

        var mockUserSessionService = new Mock<IUserSessionService>();
        mockUserSessionService.Setup(s => s.GetUser()).ReturnsAsync(CreateSuperAdminSession(superAdminUser));

        var mockConfiguration = new Mock<IConfiguration>();
        
        AdminService service = new(context, mockConfiguration.Object, mockUserSessionService.Object);

        var nonExistentUserId = Guid.NewGuid();
        var organisations = new List<string> { "Organization A" };

        // Act & Assert
        Exception exception = await Assert.ThrowsAsync<Exception>(() => 
            service.AssignOrganisationsToUser(nonExistentUserId, organisations));
        Assert.Contains($"User with ID {nonExistentUserId} not found", exception.Message);
    }

    [Fact]
    public async Task AssignOrganisationsToUser_WithNonSuperAdminUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        string dbName = $"AssignOrganisationsToUser_Unauthorized_{Guid.NewGuid()}";
        ConfluxContext context = CreateContext(dbName);

        var regularUser = CreateUserWithPerson(Guid.NewGuid(), "Regular User", "user@test.com", PermissionLevel.User);
        context.People.Add(regularUser.Person!);
        context.Users.Add(regularUser);
        await context.SaveChangesAsync();

        var mockUserSessionService = new Mock<IUserSessionService>();
        mockUserSessionService.Setup(s => s.GetUser()).ReturnsAsync(CreateRegularUserSession(regularUser));

        var mockConfiguration = new Mock<IConfiguration>();
        
        AdminService service = new(context, mockConfiguration.Object, mockUserSessionService.Object);

        var organisations = new List<string> { "Organization A" };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
            service.AssignOrganisationsToUser(Guid.NewGuid(), organisations));
    }

    [Fact]
    public async Task MapUserToResponse_MapsUserCorrectly()
    {
        // Arrange
        string dbName = $"MapUserToResponse_{Guid.NewGuid()}";
        ConfluxContext context = CreateContext(dbName);

        var superAdminUser = CreateUserWithPerson(Guid.NewGuid(), "Super Admin", "admin@test.com", PermissionLevel.SuperAdmin);
        var targetUser = CreateUserWithPerson(Guid.NewGuid(), "Test User", "test@test.com", PermissionLevel.SystemAdmin);
        targetUser.SRAMId = "sram-123";
        targetUser.AssignedLectorates = ["Computer Science", "Data Science"];
        targetUser.AssignedOrganisations = ["Org A", "Org B"];

        context.People.AddRange(superAdminUser.Person!, targetUser.Person!);
        context.Users.AddRange(superAdminUser, targetUser);
        await context.SaveChangesAsync();

        var mockUserSessionService = new Mock<IUserSessionService>();
        mockUserSessionService.Setup(s => s.GetUser()).ReturnsAsync(CreateSuperAdminSession(superAdminUser));

        var mockConfiguration = new Mock<IConfiguration>();
        
        AdminService service = new(context, mockConfiguration.Object, mockUserSessionService.Object);

        // Act
        UserResponseDTO result = await service.SetUserPermissionLevel(targetUser.Id, PermissionLevel.SystemAdmin);

        // Assert
        Assert.Equal(targetUser.Id, result.Id);
        Assert.Equal("sram-123", result.SRAMId);
        Assert.Equal("test-scim", result.SCIMId);
        Assert.Equal(PermissionLevel.SystemAdmin, result.PermissionLevel);
        Assert.Equal(2, result.AssignedLectorates.Count);
        Assert.Equal(2, result.AssignedOrganisations.Count);
        
        Assert.NotNull(result.Person);
        Assert.Equal(targetUser.PersonId, result.Person.Id);
        Assert.Equal("Test User", result.Person.Name);
        Assert.Equal("test@test.com", result.Person.Email);
        Assert.Equal(targetUser.Id, result.Person.UserId);
    }
}
