// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.API.Controllers;
using Conflux.Domain;
using Conflux.Domain.Logic.DTOs;
using Conflux.Domain.Logic.DTOs.Patch;
using Conflux.Domain.Logic.Exceptions;
using Conflux.Domain.Logic.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Conflux.API.Tests.Controllers;

public class PeopleControllerTests
{
    private readonly Mock<IPeopleService> _mockPeopleService;
    private readonly PeopleController _controller;

    public PeopleControllerTests()
    {
        _mockPeopleService = new();
        _controller = new(_mockPeopleService.Object);
    }

    [Fact]
    public async Task GetPersonsByQuery_ReturnsListOfPersons()
    {
        // Arrange
        const string query = "test";
        var people = new List<Person> 
        { 
            new() { Id = Guid.NewGuid(), Name = "Test Person" },
            new() { Id = Guid.NewGuid(), Name = "Another Test" },
        };
        
        _mockPeopleService.Setup(s => s.GetPersonsByQueryAsync(query))
            .ReturnsAsync(people);

        // Act
        var result = await _controller.GetPersonsByQuery(query);

        // Assert
        var actionResult = Assert.IsType<ActionResult<List<Person>>>(result);
        var returnValue = Assert.IsType<List<Person>>(actionResult.Value);
        Assert.Equal(2, returnValue.Count);
        Assert.Contains(returnValue, p => p.Name == "Test Person");
        Assert.Contains(returnValue, p => p.Name == "Another Test");
    }

    [Fact]
    public async Task GetPersonById_WithValidId_ReturnsPerson()
    {
        // Arrange
        Guid personId = Guid.NewGuid();
        Person person = new()
            { Id = personId, Name = "Test Person" };
        
        _mockPeopleService.Setup(s => s.GetPersonByIdAsync(personId))
            .ReturnsAsync(person);

        // Act
        var result = await _controller.GetPersonById(personId);

        // Assert
        var actionResult = Assert.IsType<ActionResult<Person>>(result);
        Person returnValue = Assert.IsType<Person>(actionResult.Value);
        Assert.Equal(personId, returnValue.Id);
        Assert.Equal("Test Person", returnValue.Name);
    }

    [Fact]
    public async Task GetPersonById_WithInvalidId_ThrowsPersonNotFoundException()
    {
        // Arrange
        Guid personId = Guid.NewGuid();
        
        _mockPeopleService.Setup(s => s.GetPersonByIdAsync(personId))
            .ThrowsAsync(new PersonNotFoundException(personId));

        // Act & Assert
        await Assert.ThrowsAsync<PersonNotFoundException>(() => _controller.GetPersonById(personId));
    }

    [Fact]
    public async Task CreatePerson_ReturnsCreatedAtActionResult()
    {
        // Arrange
        PersonDTO dto = new()
        { 
            Name = "New Person",
            Email = "new@example.com",
        };
        
        Person createdPerson = new()
        { 
            Id = Guid.NewGuid(),
            Name = "New Person",
            Email = "new@example.com",
        };
        
        _mockPeopleService.Setup(s => s.CreatePersonAsync(dto))
            .ReturnsAsync(createdPerson);

        // Act
        var result = await _controller.CreatePerson(dto);

        // Assert
        var actionResult = Assert.IsType<ActionResult<Person>>(result);
        CreatedAtActionResult createdAtActionResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
        
        Assert.Equal(nameof(PeopleController.GetPersonById), createdAtActionResult.ActionName);
        Assert.Equal(createdPerson.Id, createdAtActionResult.RouteValues!["id"]);
        
        Person returnValue = Assert.IsType<Person>(createdAtActionResult.Value);
        Assert.Equal(createdPerson.Id, returnValue.Id);
        Assert.Equal("New Person", returnValue.Name);
    }

    [Fact]
    public async Task UpdatePerson_WithValidId_ReturnsPerson()
    {
        // Arrange
        Guid personId = Guid.NewGuid();
        PersonDTO dto = new()
        { 
            Name = "Updated Person",
            Email = "updated@example.com",
        };
        
        Person updatedPerson = new()
        { 
            Id = personId,
            Name = "Updated Person",
            Email = "updated@example.com",
        };
        
        _mockPeopleService.Setup(s => s.UpdatePersonAsync(personId, dto))
            .ReturnsAsync(updatedPerson);

        // Act
        var result = await _controller.UpdatePerson(personId, dto);

        // Assert
        var actionResult = Assert.IsType<ActionResult<Person>>(result);
        Person returnValue = Assert.IsType<Person>(actionResult.Value);
        Assert.Equal(personId, returnValue.Id);
        Assert.Equal("Updated Person", returnValue.Name);
        Assert.Equal("updated@example.com", returnValue.Email);
    }

    [Fact]
    public async Task UpdatePerson_WithInvalidId_ThrowsPersonNotFoundException()
    {
        // Arrange
        Guid personId = Guid.NewGuid();
        PersonDTO dto = new()
            { Name = "Updated Person" };
        
        _mockPeopleService.Setup(s => s.UpdatePersonAsync(personId, dto))
            .ThrowsAsync(new PersonNotFoundException(personId));

        // Act & Assert
        await Assert.ThrowsAsync<PersonNotFoundException>(() => _controller.UpdatePerson(personId, dto));
    }

    [Fact]
    public async Task PatchPerson_WithValidId_ReturnsPerson()
    {
        // Arrange
        Guid personId = Guid.NewGuid();
        PersonPatchDTO dto = new()
            { Email = "patched@example.com" };
        
        Person patchedPerson = new()
        { 
            Id = personId,
            Name = "Original Name",
            Email = "patched@example.com",
        };
        
        _mockPeopleService.Setup(s => s.PatchPersonAsync(personId, dto))
            .ReturnsAsync(patchedPerson);

        // Act
        var result = await _controller.PatchPerson(personId, dto);

        // Assert
        var actionResult = Assert.IsType<ActionResult<Person>>(result);
        Person returnValue = Assert.IsType<Person>(actionResult.Value);
        Assert.Equal(personId, returnValue.Id);
        Assert.Equal("Original Name", returnValue.Name);
        Assert.Equal("patched@example.com", returnValue.Email);
    }
    
    [Fact]
    public async Task PatchPerson_WithInvalidId_ThrowsPersonNotFoundException()
    {
        // Arrange
        Guid personId = Guid.NewGuid();
        PersonPatchDTO dto = new()
            { Email = "patched@example.com" };
        
        _mockPeopleService.Setup(s => s.PatchPersonAsync(personId, dto))
            .ThrowsAsync(new PersonNotFoundException(personId));

        // Act & Assert
        await Assert.ThrowsAsync<PersonNotFoundException>(() => _controller.PatchPerson(personId, dto));
    }

    [Fact]
    public async Task DeletePerson_WithValidId_ReturnsNoContent()
    {
        // Arrange
        Guid personId = Guid.NewGuid();
        
        _mockPeopleService.Setup(s => s.DeletePersonAsync(personId))
            .Returns(Task.CompletedTask);

        // Act
        IActionResult result = await _controller.DeletePerson(personId);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeletePerson_WithInvalidId_ThrowsPersonNotFoundException()
    {
        // Arrange
        Guid personId = Guid.NewGuid();
        
        _mockPeopleService.Setup(s => s.DeletePersonAsync(personId))
            .ThrowsAsync(new PersonNotFoundException(personId));

        // Act & Assert
        await Assert.ThrowsAsync<PersonNotFoundException>(() => _controller.DeletePerson(personId));
    }

    [Fact]
    public async Task DeletePerson_WithContributors_ThrowsPersonHasContributorsException()
    {
        // Arrange
        Guid personId = Guid.NewGuid();
        
        _mockPeopleService.Setup(s => s.DeletePersonAsync(personId))
            .ThrowsAsync(new PersonHasContributorsException(personId));

        // Act & Assert
        await Assert.ThrowsAsync<PersonHasContributorsException>(() => _controller.DeletePerson(personId));
    }
}