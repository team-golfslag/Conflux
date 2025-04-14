// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
using Conflux.Domain.Logic.DTOs;
using Conflux.Domain.Logic.Exceptions;
using Conflux.Domain.Logic.Services;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace Conflux.Domain.Logic.Tests.Services;

public class ContributorServiceTests : IAsyncLifetime
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
        ContributorService contributorService = new(_context);

        // Create a test person
        ContributorPostDto dto = new()
        {
            Name = "John Doe",
        };

        // Act
        Contributor contributor = await contributorService.CreateContributorAsync(dto);

        // Assert
        Assert.NotNull(contributor);
        Assert.Single(await _context.Contributors.Where(p => p.Id == contributor.Id).ToListAsync());
    }

    [Fact]
    public async Task GetPersonById_ShouldReturnPerson_WhenPersonExists()
    {
        // Arrange
        ContributorService contributorService = new(_context);

        Guid personId = Guid.NewGuid();

        // Insert a test person
        Contributor testContributor = new()
        {
            Id = personId,
            Name = "Test Person",
        };

        _context.Contributors.Add(testContributor);
        await _context.SaveChangesAsync();

        // Act
        Contributor contributor = await contributorService.GetContributorByIdAsync(personId);

        // Assert
        Assert.NotNull(contributor);
        Assert.Equal(testContributor.Name, contributor.Name);
    }

    [Fact]
    public async Task GetPersonById_ShouldReturnNotFound_WhenPersonDoesNotExist()
    {
        // Arrange
        ContributorService contributorService = new(_context);

        Guid personId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ContributorNotFoundException>(() => contributorService.GetContributorByIdAsync(personId));
    }

    [Fact]
    public async Task CreatePerson_ShouldCreatePerson()
    {
        // Arrange
        ContributorService contributorService = new(_context);
        // Create a test person
        ContributorPostDto testContributor = new()
        {
            Name = "Test Person",
        };

        // Act
        Contributor contributor = await contributorService.CreateContributorAsync(testContributor);
        await _context.SaveChangesAsync();
        Contributor retrievedContributor = await _context.Contributors.SingleAsync(p => p.Id == contributor.Id);

        // Assert
        Assert.NotNull(contributor);
        Assert.Equal(retrievedContributor.Name, contributor.Name);
        Assert.Equal(retrievedContributor.Id, contributor.Id);
        Assert.Equal(testContributor.Name, contributor.Name);
    }

    [Fact]
    public async Task UpdatePersonAsync_ShouldUpdateName()
    {
        // Arrange
        ContributorService contributorService = new(_context);
        Guid personId = Guid.NewGuid();
        Contributor testContributor = new()
        {
            Id = personId,
            Name = "Original Name",
        };
        _context.Contributors.Add(testContributor);
        await _context.SaveChangesAsync();

        // Create a PersonPutDTO with a new name
        ContributorPutDto updateDto = new()
        {
            Name = "Updated Name",
        };

        // Act
        Contributor updatedContributor = await contributorService.UpdateContributorAsync(personId, updateDto);

        // Assert
        Assert.NotNull(updatedContributor);
        Assert.Equal("Updated Name", updatedContributor.Name);
        Assert.Equal(personId, updatedContributor.Id);
    }

    [Fact]
    public async Task PatchPersonAsync_ShouldUpdateName_WhenNameProvided()
    {
        // Arrange
        ContributorService contributorService = new(_context);
        Guid personId = Guid.NewGuid();
        Contributor testContributor = new()
        {
            Id = personId,
            Name = "Original Name",
        };
        _context.Contributors.Add(testContributor);
        await _context.SaveChangesAsync();

        // Create a PersonPatchDTO with a new name
        ContributorPatchDto patchDto = new()
        {
            Name = "Patched Name",
        };

        // Act
        Contributor patchedContributor = await contributorService.PatchContributorAsync(personId, patchDto);

        // Assert
        Assert.NotNull(patchedContributor);
        Assert.Equal("Patched Name", patchedContributor.Name);
        Assert.Equal(personId, patchedContributor.Id);
    }

    [Fact]
    public async Task PatchPersonAsync_ShouldNotChangeName_WhenNameIsNull()
    {
        // Arrange
        ContributorService contributorService = new(_context);
        Guid personId = Guid.NewGuid();
        Contributor testContributor = new()
        {
            Id = personId,
            Name = "Original Name",
        };
        _context.Contributors.Add(testContributor);
        await _context.SaveChangesAsync();

        // Create a PersonPatchDTO with no name provided (null)
        ContributorPatchDto patchDto = new()
        {
            Name = null,
        };

        // Act
        Contributor patchedContributor = await contributorService.PatchContributorAsync(personId, patchDto);

        // Assert
        Assert.NotNull(patchedContributor);
        Assert.Equal("Original Name", patchedContributor.Name);
        Assert.Equal(personId, patchedContributor.Id);
    }
}
