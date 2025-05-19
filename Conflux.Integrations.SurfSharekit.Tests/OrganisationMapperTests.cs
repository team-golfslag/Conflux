// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain;
using Conflux.Integrations.SURFSharekit;
using SURFSharekit.Net.Models.RepoItem;

namespace Conflux.Integrations.SurfSharekit.Tests;

public class OrganisationMapperTests
{
    [Fact]
    public void MapOrganisation_ShouldReturnOrganisation_WhenValidOwner()
    {
        // Arrange
        SURFSharekitOwner owner = new()
        {
            Id = "dummy-id",
            Name = "Hogeschool Utrecht",
            Type = "organisation",
        };
        
        // Act
        Organisation? organisation = OrganisationMapper.MapOrganisation(owner);
        
        // Assert
        Assert.NotNull(organisation);
        Assert.Equal("Hogeschool Utrecht", organisation.Name);
    }

    [Fact]
    public void MapOrganisation_ShouldReturnNull_WhenNameIsNull()
    {
        // Arrange
        SURFSharekitOwner owner = new()
        {
            Id = "dummy-id",
            Name = null,
            Type = "organisation",
        };
        
        // Act
        Organisation? organisation = OrganisationMapper.MapOrganisation(owner);
        
        // Assert
        Assert.Null(organisation);
    }
}
