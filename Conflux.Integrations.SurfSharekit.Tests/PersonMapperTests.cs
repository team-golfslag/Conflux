// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain;
using Conflux.Integrations.SURFSharekit;
using SURFSharekit.Net.Models.RepoItem;

namespace Conflux.Integrations.SurfSharekit.Tests;

public class PersonMapperTests
{
    [Fact]
    public void MapPerson_ShouldReturnPerson_WhenPersonIsValid()
    {
        // Arrange
        SURFSharekitPerson person = new()
        {
            Id = "dummy-id",
            Name = "Henk-Bert Bert-Henk Bert-Jan Max van Gent",
            Email = "H.B.B.M.van.gent@uu.nl",
            Orcid = "0008-0009-1234-1234",
        };
        
        // Act
        Person? mappedPerson = PersonMapper.MapPerson(person);
        
        // Assert
        Assert.NotNull(mappedPerson);
        Assert.Equal("Henk-Bert Bert-Henk Bert-Jan Max van Gent", mappedPerson.Name);
        Assert.Equal("H.B.B.M.van.gent@uu.nl", mappedPerson.Email);
        Assert.Equal("0008-0009-1234-1234", mappedPerson.ORCiD);
    }

    [Fact]
    public void MapPerson_ShouldReturnNull_WhenNameIsNull()
    {
        // Arrange
        SURFSharekitPerson person = new()
        {
            Id = "dummy-id",
            Name = null,
            Email = "H.B.B.M.van.gent@uu.nl",
            Orcid = "0008-0009-1234-1234",
        };
        
        // Act
        Person? organisation = PersonMapper.MapPerson(person);
        
        // Assert
        Assert.Null(organisation);
    }
}
