using Conflux.API.DTOs;
using Conflux.Domain;
using Xunit;

namespace Conflux.API.Tests;

public class PersonDTOTests
{
    /// <summary>
    /// Given a valid PersonDTO with Name,
    /// When the ToPerson() method is called,
    /// Then a new Person is created with a new Id and the same Name.
    /// </summary>
    [Fact]
    public void ToPerson_ShouldConvertDTOToPerson()
    {
        // Arrange
        PersonDto dto = new()
        {
            Name = "John Doe",
        };
        
        // Act 
        Person person = dto.ToPerson();
        
        // Assert
        Assert.NotNull(person);
        Assert.Equal(dto.Name, person.Name);
        Assert.NotEqual(Guid.Empty, person.Id);
    }

    public void ProjectDTO_ShouldGetSet()
    {
        // Arrange
        Guid personId = Guid.NewGuid();
        
        PersonDto dto = new()
        {
            Id = personId,
            Name = "John Doe",
        };
        
        // Act
        
        // Assert
        Assert.Equal(personId, dto.Id);
    }
}
