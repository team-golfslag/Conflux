// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Reflection;
using Conflux.Domain;
using Conflux.Domain.Session;
using NWOpen.Net.Models;
using Xunit;
using Product = Conflux.Domain.Product;
using Project = Conflux.Domain.Project;
using NwOpenProject = NWOpen.Net.Models.Project;

namespace Conflux.Integrations.NWOpen.Tests;

public class NwOpenMapperTests
{
    public NwOpenMapperTests()
    {
        // Clear any leftover data from static lists before each test
        ResetStaticLists();
    }

    /// <summary>
    /// Given an empty list of projects
    /// When mapping the projects
    /// Then an empty seed data object is returned.
    /// </summary>
    [Fact]
    public void MapProjects_EmptyList_ReturnsEmptySeedData()
    {
        // Arrange
        List<NwOpenProject> emptyProjects = [];

        // Act
        SeedData seedData = NwOpenMapper.MapProjects(emptyProjects);

        // Assert
        Assert.Empty(seedData.Organisations);
        Assert.Empty(seedData.Contributors);
        Assert.Empty(seedData.Products);
        Assert.Empty(seedData.Projects);
        Assert.Empty(seedData.Users);
        Assert.Empty(seedData.UserRoles);
    }

    /// <summary>
    /// Given a single project with a product and a member
    /// When mapping the project
    /// Then the seed data object contains the project, product, and member
    /// </summary>
    [Fact]
    public void MapProjects_SingleProjectWithProductAndMember_MapsAllFields()
    {
        // Arrange
        NwOpenProject singleProject = new()
        {
            Title = "Test Project",
            SummaryNl = "Summary",
            StartDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2023, 12, 31, 23, 59, 59, DateTimeKind.Utc),
            Products =
            [
                new()
                {
                    Title = "Prod1",
                    UrlOpenAccess = "http://example.com",
                },
            ],
            ProjectMembers =
            [
                new ProjectMember
                {
                    FirstName = "John",
                    LastName = "Doe",
                    Organisation = "TestOrg",
                    Role = "role",
                },
            ],
            ProjectId = "",
            FundingScheme = "",
            Department = "",
            SubDepartment = "",
            SummaryEn = "",
        };

        // Act
        SeedData result = NwOpenMapper.MapProjects([
            singleProject,
        ]);

        // Assert
        Assert.Single(result.Projects);
        Project mappedProject = result.Projects[0];
        Assert.Equal("Test Project", mappedProject.Titles[0].Text);
        List<ProjectDescription> dutchDescriptions =
            mappedProject.Descriptions.Where(d => d.Language!.Id == "nld").ToList();
        Assert.Single(dutchDescriptions);
        Assert.Equal("Summary", dutchDescriptions[0].Text);
        Assert.Equal(new(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc), mappedProject.StartDate);
        Assert.Equal(new(2023, 12, 31, 23, 59, 59, DateTimeKind.Utc), mappedProject.EndDate);

        Assert.Single(result.Products);
        Product mappedProduct = result.Products[0];
        Assert.Equal("Prod1", mappedProduct.Title);
        Assert.Equal("http://example.com", mappedProduct.Url);

        Assert.Single(result.Contributors);
        Contributor mappedUser = result.Contributors[0];
        Assert.True(mappedUser.Contact);

        Assert.Single(result.Organisations);
        Organisation mappedOrganisation = result.Organisations[0];
        Assert.Equal("TestOrg", mappedOrganisation.Name);

        // Check for development user
        Assert.NotEmpty(result.Users);
        User devUser = result.Users.Single(u => u.Id == UserSession.DevelopmentUserId);
        Assert.Equal("Development User", devUser.Name);

        // Check for user roles
        Assert.Equal(2, result.UserRoles.Count);
        Assert.Contains(result.UserRoles, r => r.Type == UserRoleType.Admin);
        Assert.Contains(result.UserRoles, r => r.Type == UserRoleType.User);

        // Verify development user has both roles
        Assert.Equal(2, devUser.Roles.Count);
        Assert.Contains(devUser.Roles, r => r.Type == UserRoleType.Admin);
        Assert.Contains(devUser.Roles, r => r.Type == UserRoleType.User);
    }

    /// <summary>
    /// Given a project with multiple products
    /// When mapping the project
    /// Then the seed data object contains all products
    /// </summary>
    [Fact]
    public void MapProjects_MultipleProjectsWithSameProductUrl_OnlyOneProductCreated()
    {
        // Arrange
        List<NwOpenProject> projectList =
        [
            new()
            {
                Products =
                [
                    new()
                    {
                        Title = "First Title",
                        UrlOpenAccess = "http://example.com",
                    },
                ],
                ProjectId = "",
                Title = "",
                FundingScheme = "",
                Department = "",
                SubDepartment = "",
                SummaryNl = "",
                SummaryEn = "",
            },

            new()
            {
                Products =
                [
                    new()
                    {
                        Title = "Second Title",
                        UrlOpenAccess = "http://example.com",
                    },
                ],
                ProjectId = "",
                Title = "",
                FundingScheme = "",
                Department = "",
                SubDepartment = "",
                SummaryNl = "",
                SummaryEn = "",
            },
        ];

        // Act
        SeedData seedData = NwOpenMapper.MapProjects(projectList);

        // Assert
        Assert.Equal(2, seedData.Projects.Count);
        Assert.Single(seedData.Products);

        // Check for development user
        Assert.NotEmpty(seedData.Users);
        User devUser = seedData.Users.Single(u => u.Id == UserSession.DevelopmentUserId);

        // Check for user roles - should have 2 roles per project = 4 total
        Assert.Equal(4, seedData.UserRoles.Count);
        Assert.Equal(2, seedData.UserRoles.Count(r => r.Type == UserRoleType.Admin));
        Assert.Equal(2, seedData.UserRoles.Count(r => r.Type == UserRoleType.User));

        // Verify each project has the development user with both roles
        foreach (Project project in seedData.Projects)
        {
            Assert.Contains(project.Users, u => u.Id == UserSession.DevelopmentUserId);
            Assert.Equal(2, seedData.UserRoles.Count(r => r.ProjectId == project.Id));
            Assert.Single(seedData.UserRoles, r => r.ProjectId == project.Id && r.Type == UserRoleType.Admin);
            Assert.Single(seedData.UserRoles, r => r.ProjectId == project.Id && r.Type == UserRoleType.User);
        }

        // Verify that the development user has all roles (2 per project = 4 total)
        Assert.Equal(4, devUser.Roles.Count);
        Assert.Equal(2, devUser.Roles.Count(r => r.Type == UserRoleType.Admin));
        Assert.Equal(2, devUser.Roles.Count(r => r.Type == UserRoleType.User));

        // Verify that roles in UserRoles list match the ones in the user's Roles list
        foreach (UserRole role in seedData.UserRoles)
            Assert.Contains(devUser.Roles, r => r.Id == role.Id);
    }

    /// <summary>
    /// Helper to reset all the static lists in NwOpenMapper before each test.
    /// </summary>
    private static void ResetStaticLists()
    {
        // Use reflection because the properties are static + private getters
        Type mapperType = typeof(NwOpenMapper);

        ClearListProperty<Organisation>(mapperType, "Organisations");
        ClearListProperty<Contributor>(mapperType, "Contributors");
        ClearListProperty<Product>(mapperType, "Products");
        ClearListProperty<Project>(mapperType, "Projects");
        ClearListProperty<Person>(mapperType, "People");
        ClearListProperty<User>(mapperType, "Users");
        ClearListProperty<UserRole>(mapperType, "UserRoles");
    }

    /// <summary>
    /// Helper to clear a static list property in a class.
    /// </summary>
    /// <param name="targetType">The type of the class containing the list property. </param>
    /// <param name="propertyName">The name of the list property to clear.</param>
    /// <typeparam name="T">The type of the list.</typeparam>
    private static void ClearListProperty<T>(Type targetType, string propertyName)
    {
        PropertyInfo? propInfo = targetType.GetProperty(propertyName,
            BindingFlags.Static |
            BindingFlags.Public |
            BindingFlags.NonPublic);

        if (propInfo is null) return;

        IList<T>? listRef = propInfo.GetValue(null) as IList<T>;
        listRef?.Clear();
    }
}
