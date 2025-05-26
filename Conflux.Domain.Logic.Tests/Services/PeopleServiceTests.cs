// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
using Conflux.Domain.Logic.DTOs;
using Conflux.Domain.Logic.DTOs.Patch;
using Conflux.Domain.Logic.Exceptions;
using Conflux.Domain.Logic.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Conflux.Domain.Logic.Tests.Services;

public class PeopleServiceTests : IAsyncLifetime
{
    private ConfluxContext _context = null!;
    private PeopleService _service = null!;

    public async Task InitializeAsync()
    {
        // Use a new in-memory database for each test run
        var options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new(options);
        await _context.Database.EnsureCreatedAsync();

        // seed some persons
        _context.People.AddRange(
            new Person
            {
                Id = Guid.NewGuid(),
                Name = "John Doe",
                GivenName = "John",
                FamilyName = "Doe",
                Email = "john@example.com",
            },
            new Person
            {
                Id = Guid.NewGuid(),
                Name = "Jane Smith",
                GivenName = "Jane",
                FamilyName = "Smith",
                Email = "jane@example.com",
            },
            new Person
            {
                Id = Guid.NewGuid(),
                Name = "Bob Johnson",
                GivenName = "Bob",
                FamilyName = "Johnson",
                Email = "bob@example.com",
            }
        );
        await _context.SaveChangesAsync();

        _service = new(_context);
    }

    public async Task DisposeAsync()
    {
        // clean up
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }

    [Fact]
    public async Task GetPersonsByQueryAsync_WithNullQuery_ReturnsAllPeople()
    {
        var result = await _service.GetPersonsByQueryAsync(null);

        Assert.Equal(3, result.Count);
        Assert.Contains(result, p => p.Name == "John Doe");
        Assert.Contains(result, p => p.Name == "Jane Smith");
        Assert.Contains(result, p => p.Name == "Bob Johnson");
    }

    [Fact]
    public async Task GetPersonsByQueryAsync_WithEmptyQuery_ReturnsAllPeople()
    {
        var result = await _service.GetPersonsByQueryAsync(string.Empty);
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetPersonsByQueryAsync_WithValidQuery_ReturnsFilteredPeople()
    {
        var result = await _service.GetPersonsByQueryAsync("John");

        // should match both "John Doe" and "Bob Johnson"
        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.Name == "John Doe");
        Assert.Contains(result, p => p.Name == "Bob Johnson");
    }

    [Fact]
    public async Task GetPersonByIdAsync_WithValidId_ReturnsPerson()
    {
        Person target = await _context.People.FirstAsync(p => p.Name == "Jane Smith");

        Person result = await _service.GetPersonByIdAsync(target.Id);

        Assert.Equal(target.Id, result.Id);
        Assert.Equal("Jane Smith", result.Name);
    }

    [Fact]
    public async Task GetPersonByIdAsync_WithInvalidId_ThrowsPersonNotFoundException()
    {
        await Assert.ThrowsAsync<PersonNotFoundException>(() => _service.GetPersonByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreatePersonAsync_AddsPersonToDatabase()
    {
        PersonDTO dto = new()
        {
            Name = "Alice Wonderland",
            GivenName = "Alice",
            FamilyName = "Wonderland",
            Email = "alice@example.com",
        };

        Person created = await _service.CreatePersonAsync(dto);

        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal(dto.Name, created.Name);

        // Verify it persisted
        Person? fetched = await _context.People.FindAsync(created.Id);
        Assert.NotNull(fetched);
        Assert.Equal("Alice Wonderland", fetched!.Name);
    }
    
    [Fact]
    public async Task GetPersonByOrcidIdAsync_WithValidOrcidId_ReturnsPerson()
    {
        // Arrange
        const string testOrcid = "0000-0001-2345-6789";
        Person person = await _context.People.FirstAsync();
        person.ORCiD = testOrcid;
        await _context.SaveChangesAsync();

        // Act
        Person? result = await _service.GetPersonByOrcidIdAsync(testOrcid);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(person.Id, result!.Id);
        Assert.Equal(testOrcid, result.ORCiD);
    }

    [Fact]
    public async Task GetPersonByOrcidIdAsync_WithEmptyOrcidId_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.GetPersonByOrcidIdAsync(""));
    
        Assert.Equal("orcidId", exception.ParamName);
    
        // Also test null
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.GetPersonByOrcidIdAsync(null!));
    }

    [Fact]
    public async Task UpdatePersonAsync_WithValidId_UpdatesPerson()
    {
        Person target = await _context.People.FirstAsync();
        PersonDTO dto = new()
        {
            Name = "Updated",
            GivenName = "Up",
            FamilyName = "Date",
            Email = "updated@example.com",
        };

        Person updated = await _service.UpdatePersonAsync(target.Id, dto);

        Assert.Equal(dto.Name, updated.Name);
        Assert.Equal(dto.Email, updated.Email);

        // persisted?
        Person? persisted = await _context.People.FindAsync(target.Id);
        Assert.Equal("Updated", persisted!.Name);
    }

    [Fact]
    public async Task PatchPersonAsync_WithValidId_PatchesEmailOnly()
    {
        Person target = await _context.People.FirstAsync();
        string originalName = target.Name;

        PersonPatchDTO patch = new()
        {
            Email = "patched@example.com",
        };

        Person patched = await _service.PatchPersonAsync(target.Id, patch);

        Assert.Equal(originalName, patched.Name);
        Assert.Equal("patched@example.com", patched.Email);

        Person? persisted = await _context.People.FindAsync(target.Id);
        Assert.Equal("patched@example.com", persisted!.Email);
    }

    [Fact]
    public async Task DeletePersonAsync_WithNoContributors_DeletesPerson()
    {
        // pick a seeded person
        Person target = await _context.People.FirstAsync();

        await _service.DeletePersonAsync(target.Id);

        bool exists = await _context.People.AnyAsync(p => p.Id == target.Id);
        Assert.False(exists);
    }

    [Fact]
    public async Task DeletePersonAsync_WithContributors_ThrowsPersonHasContributorsException()
    {
        // seed a new person + a contributor for them
        Person person = new()
        {
            Id = Guid.NewGuid(),
            Name = "X Y",
        };
        await _context.People.AddAsync(person);

        Project project = new()
        {
            Id = Guid.NewGuid(),
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
        };
        await _context.Projects.AddAsync(project);

        Contributor contrib = new()
        {
            PersonId = person.Id,
            ProjectId = project.Id,
        };
        await _context.Contributors.AddAsync(contrib);
        await _context.SaveChangesAsync();

        await Assert.ThrowsAsync<PersonHasContributorsException>(() => _service.DeletePersonAsync(person.Id));
    }
}
