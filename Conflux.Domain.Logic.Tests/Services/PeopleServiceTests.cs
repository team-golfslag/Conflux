using Conflux.Data;
using Conflux.Domain.Logic.DTOs;
using Conflux.Domain.Logic.Exceptions;
using Conflux.Domain.Logic.Services;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace Conflux.Domain.Logic.Tests.Services;

public class PeopleServiceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();
    private ConfluxContext _context;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        var options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        ConfluxContext context = new(options);
        await context.Database.EnsureCreatedAsync();
        _context = context;
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task CreatePersonAsync_ShouldCreatePerson()
    {
        // Arrange
        PeopleService peopleService = new(_context);

        // Create a test person
        PersonPostDTO dto = new()
        {
            Name = "John Doe",
        };

        // Act
        Person person = await peopleService.CreatePersonAsync(dto);

        // Assert
        Assert.NotNull(person);
        Assert.Single(await _context.People.Where(p => p.Id == person.Id).ToListAsync());
    }

    [Fact]
    public async Task GetPersonById_ShouldReturnPerson_WhenPersonExists()
    {
        // Arrange
        PeopleService peopleService = new(_context);

        Guid personId = Guid.NewGuid();

        // Insert a test person
        Person testPerson = new()
        {
            Id = personId,
            Name = "Test Person",
        };

        _context.People.Add(testPerson);
        await _context.SaveChangesAsync();

        // Act
        Person person = await peopleService.GetPersonByIdAsync(personId);

        // Assert
        Assert.NotNull(person);
        Assert.Equal(testPerson.Name, person.Name);
    }

    [Fact]
    public async Task GetPersonById_ShouldReturnNotFound_WhenPersonDoesNotExist()
    {
        // Arrange
        PeopleService peopleService = new(_context);

        Guid personId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<PersonNotFoundException>(() => peopleService.GetPersonByIdAsync(personId));
    }

    [Fact]
    public async Task CreatePerson_ShouldCreatePerson()
    {
        // Arrange
        PeopleService peopleService = new(_context);
        // Create a test person
        PersonPostDTO testPerson = new()
        {
            Name = "Test Person",
        };

        // Act
        Person person = await peopleService.CreatePersonAsync(testPerson);
        await _context.SaveChangesAsync();
        Person retrievedPerson = await _context.People.SingleAsync(p => p.Id == person.Id);

        // Assert
        Assert.NotNull(person);
        Assert.Equal(retrievedPerson.Name, person.Name);
        Assert.Equal(retrievedPerson.Id, person.Id);
        Assert.Equal(testPerson.Name, person.Name);
    }

    [Fact]
    public async Task UpdatePersonAsync_ShouldUpdateName()
    {
        // Arrange
        PeopleService peopleService = new(_context);
        Guid personId = Guid.NewGuid();
        Person testPerson = new()
        {
            Id = personId,
            Name = "Original Name",
        };
        _context.People.Add(testPerson);
        await _context.SaveChangesAsync();

        // Create a PersonPutDTO with a new name
        PersonPutDTO updateDto = new()
        {
            Name = "Updated Name",
        };

        // Act
        Person updatedPerson = await peopleService.UpdatePersonAsync(personId, updateDto);

        // Assert
        Assert.NotNull(updatedPerson);
        Assert.Equal("Updated Name", updatedPerson.Name);
        Assert.Equal(personId, updatedPerson.Id);
    }

    [Fact]
    public async Task PatchPersonAsync_ShouldUpdateName_WhenNameProvided()
    {
        // Arrange
        PeopleService peopleService = new(_context);
        Guid personId = Guid.NewGuid();
        Person testPerson = new()
        {
            Id = personId,
            Name = "Original Name",
        };
        _context.People.Add(testPerson);
        await _context.SaveChangesAsync();

        // Create a PersonPatchDTO with a new name
        PersonPatchDTO patchDto = new()
        {
            Name = "Patched Name",
        };

        // Act
        Person patchedPerson = await peopleService.PatchPersonAsync(personId, patchDto);

        // Assert
        Assert.NotNull(patchedPerson);
        Assert.Equal("Patched Name", patchedPerson.Name);
        Assert.Equal(personId, patchedPerson.Id);
    }

    [Fact]
    public async Task PatchPersonAsync_ShouldNotChangeName_WhenNameIsNull()
    {
        // Arrange
        PeopleService peopleService = new(_context);
        Guid personId = Guid.NewGuid();
        Person testPerson = new()
        {
            Id = personId,
            Name = "Original Name",
        };
        _context.People.Add(testPerson);
        await _context.SaveChangesAsync();

        // Create a PersonPatchDTO with no name provided (null)
        PersonPatchDTO patchDto = new()
        {
            Name = null,
        };

        // Act
        Person patchedPerson = await peopleService.PatchPersonAsync(personId, patchDto);

        // Assert
        Assert.NotNull(patchedPerson);
        Assert.Equal("Original Name", patchedPerson.Name);
        Assert.Equal(personId, patchedPerson.Id);
    }
}
