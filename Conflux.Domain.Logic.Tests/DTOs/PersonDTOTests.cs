// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain.Logic.DTOs;
using Xunit;

namespace Conflux.Domain.Logic.Tests.DTOs;

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
        PersonPostDTO dto = new()
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
