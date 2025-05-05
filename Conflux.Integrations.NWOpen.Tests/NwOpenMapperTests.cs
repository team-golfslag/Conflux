// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Reflection;
using Conflux.Domain;
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
        var emptyProjects = new List<NwOpenProject>();

        // Act
        SeedData seedData = NwOpenMapper.MapProjects(emptyProjects);

        // Assert
        Assert.Empty(seedData.Organisations);
        Assert.Empty(seedData.Contributors);
        Assert.Empty(seedData.Products);
        Assert.Empty(seedData.Projects);
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
        List<ProjectDescription> dutchDescriptions = mappedProject.Descriptions.Where(d => d.Language!.Id == "nld").ToList();
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
        Assert.False(mappedUser.Contact);

        Assert.Single(result.Organisations);
        Organisation mappedOrganisation = result.Organisations[0];
        Assert.Equal("TestOrg", mappedOrganisation.Name);
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
        var projectList = new List<NwOpenProject>
        {
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
        };

        // Act
        SeedData seedData = NwOpenMapper.MapProjects(projectList);

        // Assert
        Assert.Equal(2, seedData.Projects.Count);
        Assert.Single(seedData.Products);
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

        var listRef = propInfo.GetValue(null) as IList<T>;
        listRef?.Clear();
    }
}
