using System.Reflection;
using Conflux.Domain;
using Conflux.RepositoryConnections.NWOpen;
using NWOpen.Net.Models;
using Xunit;
using Product = Conflux.Domain.Product;
using Project = Conflux.Domain.Project;
using NwOpenProject = NWOpen.Net.Models.Project;

namespace Conflux.RepositoryConnections.Tests;

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
        Assert.Empty(seedData.Parties);
        Assert.Empty(seedData.People);
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
            EndDate = new DateTime(2023, 12, 31, 0, 0, 0, DateTimeKind.Utc),
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
        Assert.Equal("Test Project", mappedProject.Title);
        Assert.Equal("Summary", mappedProject.Description);
        Assert.Equal(new DateOnly(2023, 1, 1), mappedProject.StartDate);
        Assert.Equal(new DateOnly(2023, 12, 31), mappedProject.EndDate);

        Assert.Single(result.Products);
        Product mappedProduct = result.Products[0];
        Assert.Equal("Prod1", mappedProduct.Title);
        Assert.Equal("http://example.com", mappedProduct.Url);

        Assert.Single(result.People);
        Person mappedPerson = result.People[0];
        Assert.Equal("John Doe", mappedPerson.Name);

        Assert.Single(result.Parties);
        Party mappedParty = result.Parties[0];
        Assert.Equal("TestOrg", mappedParty.Name);
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
    /// Test to see if valid urls of products are converted correctly and return true.
    /// Also tests urls that should return false
    /// </summary>
    [Fact]
    public void ValidUrlsTestDoi()
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
                        Title = "title 1",
                        UrlOpenAccess = "doi: 10.1000/182",
                    },
                    new()
                    {
                        Title = "title 2",
                        UrlOpenAccess = "doi: ThisShouldFail:)",
                    },
                    new()
                    {
                        Title = "title 3",
                        UrlOpenAccess = "https://www.doi.org/the-identifier/resources/handbook/",
                    },
                    new()
                    {
                        Title = "title 4",
                        UrlOpenAccess = "http://localhost:3000/myOwnServer",
                    },
                    new()
                    {
                        Title = "title 5",
                        UrlOpenAccess = null,
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
        
        // Asserts
        Assert.True(seedData.Products[0].IsValidUrl);
        Assert.Equal("https://doi.org/10.1000/182", seedData.Products[0].Url);
        
        Assert.False(seedData.Products[1].IsValidUrl);
        Assert.Equal("https://doi.org/ThisShouldFail:)", seedData.Products[1].Url);
        
        Assert.True(seedData.Products[2].IsValidUrl);
        Assert.Equal("https://www.doi.org/the-identifier/resources/handbook/", seedData.Products[2].Url);
        
        Assert.False(seedData.Products[3].IsValidUrl);
        Assert.Equal("http://localhost:3000/myOwnServer", seedData.Products[3].Url);
        
        Assert.False(seedData.Products[4].IsValidUrl);
        Assert.Null(seedData.Products[4].Url);
    }
    
    /// <summary>
    /// Helper to reset all the static lists in NwOpenMapper before each test.
    /// </summary>
    private static void ResetStaticLists()
    {
        // Use reflection because the properties are static + private getters
        Type mapperType = typeof(NwOpenMapper);

        ClearListProperty<Party>(mapperType, "Parties");
        ClearListProperty<Person>(mapperType, "People");
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
