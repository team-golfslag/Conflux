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

public class ContributorsServiceTests : IAsyncLifetime
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
    public async Task CreateContributorAsync_ShouldCreateContributor()
    {
        // Arrange
        ContributorsService contributorsService = new(_context);

        ContributorDTO dto = new()
        {
            Name = "John Doe",
        };

        // Act
        Contributor contributor = await contributorsService.CreateContributorAsync(dto);

        // Assert
        Assert.NotNull(contributor);
        Assert.Single(await _context.Contributors.Where(p => p.Id == contributor.Id).ToListAsync());
    }

    [Fact]
    public async Task GetContributorById_ShouldReturnContributor_WhenContributorExists()
    {
        // Arrange
        ContributorsService contributorsService = new(_context);

        Guid contributorId = Guid.NewGuid();

        Contributor testContributor = new()
        {
            Id = contributorId,
            Name = "Test Contributor",
        };

        _context.Contributors.Add(testContributor);
        await _context.SaveChangesAsync();

        // Act
        Contributor contributor = await contributorsService.GetContributorByIdAsync(contributorId);

        // Assert
        Assert.NotNull(contributor);
        Assert.Equal(testContributor.Name, contributor.Name);
    }

    [Fact]
    public async Task GetContributorById_ShouldReturnNotFound_WhenContributorDoesNotExist()
    {
        // Arrange
        ContributorsService contributorService = new(_context);

        Guid contributorId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ContributorNotFoundException>(() =>
            contributorService.GetContributorByIdAsync(contributorId));
    }

    [Fact]
    public async Task CreateContributor_ShouldCreateContributor()
    {
        // Arrange
        ContributorsService contributorsService = new(_context);
        ContributorDTO testContributor = new()
        {
            Name = "Test Contributor",
        };

        // Act
        Contributor contributor = await contributorsService.CreateContributorAsync(testContributor);
        await _context.SaveChangesAsync();
        Contributor retrievedContributor = await _context.Contributors.SingleAsync(p => p.Id == contributor.Id);

        // Assert
        Assert.NotNull(contributor);
        Assert.Equal(retrievedContributor.Name, contributor.Name);
        Assert.Equal(retrievedContributor.Id, contributor.Id);
        Assert.Equal(testContributor.Name, contributor.Name);
    }

    [Fact]
    public async Task UpdateContributorAsync_ShouldUpdateName()
    {
        // Arrange
        ContributorsService contributorsService = new(_context);
        Guid contributorId = Guid.NewGuid();
        Contributor testContributor = new()
        {
            Id = contributorId,
            Name = "Original Name",
        };
        _context.Contributors.Add(testContributor);
        await _context.SaveChangesAsync();

        ContributorDTO updateDto = new()
        {
            Name = "Updated Name",
        };

        // Act
        Contributor updatedContributor = await contributorsService.UpdateContributorAsync(contributorId, updateDto);

        // Assert
        Assert.NotNull(updatedContributor);
        Assert.Equal("Updated Name", updatedContributor.Name);
        Assert.Equal(contributorId, updatedContributor.Id);
    }

    [Fact]
    public async Task PatchContributorAsync_ShouldUpdateName_WhenNameProvided()
    {
        // Arrange
        ContributorsService contributorsService = new(_context);
        Guid contributorId = Guid.NewGuid();
        Contributor contributor = new()
        {
            Id = contributorId,
            Name = "Original Name",
        };
        _context.Contributors.Add(contributor);
        await _context.SaveChangesAsync();

        // Create a ContributorPatchDTO with a new name
        ContributorPatchDTO patchDto = new()
        {
            Name = "Patched Name",
        };

        // Act
        Contributor patchedContributor = await contributorsService.PatchContributorAsync(contributorId, patchDto);

        // Assert
        Assert.NotNull(patchedContributor);
        Assert.Equal("Patched Name", patchedContributor.Name);
        Assert.Equal(contributorId, patchedContributor.Id);
    }

    [Fact]
    public async Task PatchContributorAsync_ShouldNotChangeName_WhenNameIsNull()
    {
        // Arrange
        ContributorsService contributorsService = new(_context);
        Guid contributorId = Guid.NewGuid();

        Contributor testContributor = new()
        {
            Id = contributorId,
            Name = "Original Name",
        };
        _context.Contributors.Add(testContributor);
        await _context.SaveChangesAsync();

        ContributorPatchDTO patchDto = new()
        {
            Name = null,
        };

        // Act
        Contributor patchedContributor = await contributorsService.PatchContributorAsync(contributorId, patchDto);

        // Assert
        Assert.NotNull(patchedContributor);
        Assert.Equal("Original Name", patchedContributor.Name);
        Assert.Equal(contributorId, patchedContributor.Id);
    }
}
