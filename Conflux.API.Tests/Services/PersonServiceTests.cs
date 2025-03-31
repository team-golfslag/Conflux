using Conflux.API.DTOs;
using Conflux.API.Services;
using Conflux.Data;
using Conflux.Domain;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace Conflux.API.Tests.Services;

public class PersonServiceTests : IAsyncLifetime
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
        PersonService personService = new(_context);
        
        // Create a test person
        PersonDto dto = new()
        {
            Name = "John Doe",
        };
        
        // Act
        Person? person = await personService.CreatePersonAsync(dto);
        
        // Assert
        Assert.NotNull(person);
        Assert.Single(await _context.People.Where(p => p.Id == person.Id).ToListAsync());
    }
}
