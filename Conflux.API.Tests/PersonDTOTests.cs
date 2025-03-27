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
}
