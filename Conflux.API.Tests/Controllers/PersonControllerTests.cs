using Conflux.API.Controllers;
using Conflux.API.DTOs;
using Conflux.API.Services;
using Conflux.Data;
using Conflux.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace Conflux.API.Tests.Controllers;

public class PersonControllerTests  : IAsyncLifetime
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
    public void GetPersonById_ShouldReturnPerson_WhenPersonExists()
    {
        // Arrange
        PersonController personController = new(_context);
        
        Guid personId = Guid.NewGuid();
        
        // Insert a test person
        Person testPerson = new()
        {
            Id = personId,
            Name = "Test Person",
        };

        _context.People.Add(testPerson);
        _context.SaveChanges();
        
        // Act
        Person? person = personController.GetPersonById(personId).Value;
        
        // Assert
        Assert.NotNull(person);
        Assert.Equal(testPerson.Name, person.Name);
    }

    [Fact]
    public void GetPersonById_ShouldReturnNotFound_WhenPersonDoesNotExist()
    {
        // Arrange
        PersonController personController = new(_context);
        
        Guid personId = Guid.NewGuid();
        
        // Act
        Person? person = personController.GetPersonById(personId).Value;
        
        // Assert
        Assert.Null(person);
    }

    [Fact]
    public async Task CreatePerson_ShouldCreatePerson()
    {
        // Arrange
        PersonController personController = new(_context);
        // Create a test person
        PersonDto testPerson = new()
        {
            Name = "Test Person",
        };
        
        // Act
        await personController.CreatePerson(testPerson);
        await _context.SaveChangesAsync();
        Person person = _context.People.Single(p => p.Id == testPerson.Id);
        
        // Assert
        Assert.NotNull(person);
        Assert.Equal(testPerson.Name, person.Name);
    }
    
}
