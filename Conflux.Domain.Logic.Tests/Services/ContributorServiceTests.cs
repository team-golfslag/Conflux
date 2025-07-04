// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
using Conflux.Domain.Logic.DTOs.Requests;
using Conflux.Domain.Logic.DTOs.Responses;
using Conflux.Domain.Logic.Exceptions;
using Conflux.Domain.Logic.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Conflux.Domain.Logic.Tests.Services;

public class ContributorsServiceTests : IDisposable
{
    private readonly ConfluxContext _context = null!;

    public ContributorsServiceTests()
    {
        ServiceProvider serviceProvider = new ServiceCollection()
            .AddEntityFrameworkInMemoryDatabase()
            .BuildServiceProvider();

        DbContextOptions<ConfluxContext> options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.CreateVersion7()}")
            .UseInternalServiceProvider(serviceProvider)
            .Options;

        ConfluxContext context = new(options);
        context.Database.EnsureCreated();
        _context = context;
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task CreateContributorAsync_ShouldCreateContributor()
    {
        // Arrange
        ContributorsService contributorsService = new(_context);

        // Add project to the context
        Guid projectId = Guid.CreateVersion7();
        Project project = new()
        {
            Id = projectId,
            Titles =
            [
                new()
                {
                    Id = projectId,
                    ProjectId = projectId,
                    Text = "Test Project",
                    Type = TitleType.Primary,
                    StartDate = DateTime.UtcNow,
                },
            ],
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(1),
        };
        _context.Projects.Add(project);

        // Add person to the context
        Guid personId = Guid.CreateVersion7();
        Person person = new()
        {
            Id = personId,
            Name = "John Doe",
        };
        _context.People.Add(person);

        ContributorRequestDTO dto = new()
        {
            Leader = true,
            Contact = true,
        };

        // Act
        ContributorResponseDTO contributor = await contributorsService.CreateContributorAsync(projectId, personId, dto);

        // Assert
        Assert.NotNull(contributor);
        Assert.Single(await _context.Contributors
            .Where(p => p.PersonId == contributor.Person.Id && p.ProjectId == contributor.ProjectId).ToListAsync());
    }

    [Fact]
    public async Task GetContributorById_ShouldReturnContributor_WhenContributorExists()
    {
        // Arrange
        ContributorsService contributorsService = new(_context);

        // Add project to the context
        Guid projectId = Guid.CreateVersion7();
        Project project = new()
        {
            Id = projectId,
            Titles =
            [
                new()
                {
                    Id = projectId,
                    ProjectId = projectId,
                    Text = "Test Project",
                    Type = TitleType.Primary,
                    StartDate = DateTime.UtcNow,
                },
            ],
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(1),
        };
        _context.Projects.Add(project);

        // Add person to the context
        Guid personId = Guid.CreateVersion7();
        Person person = new()
        {
            Id = personId,
            Name = "John Doe",
        };
        _context.People.Add(person);

        Contributor testContributor = new()
        {
            ProjectId = projectId,
            PersonId = personId,
            Leader = true,
            Contact = true,
        };

        _context.Contributors.Add(testContributor);
        await _context.SaveChangesAsync();

        // Act
        ContributorResponseDTO contributor = await contributorsService.GetContributorByIdAsync(projectId, personId);

        // Assert
        Assert.NotNull(contributor);
        Assert.Equal(testContributor.PersonId, contributor.Person?.Id);
    }

    [Fact]
    public async Task GetContributorById_ShouldReturnNotFound_WhenContributorDoesNotExist()
    {
        // Arrange
        ContributorsService contributorService = new(_context);

        Guid projectId = Guid.CreateVersion7();
        Guid personId = Guid.CreateVersion7();

        // Act & Assert
        await Assert.ThrowsAsync<ContributorNotFoundException>(() =>
            contributorService.GetContributorByIdAsync(projectId, personId));
    }

    [Fact]
    public async Task CreateContributor_ShouldCreateContributor()
    {
        // Arrange
        ContributorsService contributorsService = new(_context);

        // Add project to the context
        Guid projectId = Guid.CreateVersion7();
        Project project = new()
        {
            Id = projectId,
            Titles =
            [
                new()
                {
                    Id = projectId,
                    ProjectId = projectId,
                    Text = "Test Project",
                    Type = TitleType.Primary,
                    StartDate = DateTime.UtcNow,
                },
            ],
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(1),
        };
        _context.Projects.Add(project);

        // Add person to the context
        Guid personId = Guid.CreateVersion7();
        Person person = new()
        {
            Id = personId,
            Name = "John Doe",
        };
        _context.People.Add(person);

        ContributorRequestDTO dto = new()
        {
            Leader = true,
            Contact = true,
        };

        // Act
        ContributorResponseDTO contributor =
            await contributorsService.CreateContributorAsync(projectId, person.Id, dto);
        await _context.SaveChangesAsync();
        Contributor retrievedContributor =
            await _context.Contributors.SingleAsync(p => p.ProjectId == contributor.ProjectId);

        // Assert
        Assert.NotNull(contributor);
        Assert.Equal(retrievedContributor.Leader, contributor.Leader);
        Assert.Equal(retrievedContributor.Contact, contributor.Contact);
    }

    [Fact]
    public async Task UpdateContributorAsync_ShouldUpdateName()
    {
        // Arrange
        ContributorsService contributorsService = new(_context);
        // Add project to the context
        Guid projectId = Guid.CreateVersion7();
        Project project = new()
        {
            Id = projectId,
            Titles =
            [
                new()
                {
                    Id = projectId,
                    ProjectId = projectId,
                    Text = "Test Project",
                    Type = TitleType.Primary,
                    StartDate = DateTime.UtcNow,
                },
            ],
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(1),
        };
        _context.Projects.Add(project);

        // Add person to the context
        Guid personId = Guid.CreateVersion7();
        Person person = new()
        {
            Id = personId,
            Name = "John Doe",
        };
        _context.People.Add(person);

        Contributor testContributor = new()
        {
            PersonId = personId,
            ProjectId = projectId,
            Roles =
            [
                new()
                {
                    PersonId = personId,
                    ProjectId = projectId,
                    RoleType = ContributorRoleType.Conceptualization,
                },
            ],
            Positions =
            [
                new()
                {
                    PersonId = personId,
                    ProjectId = projectId,
                    Position = ContributorPositionType.CoInvestigator,
                    StartDate = DateTime.UtcNow,
                    EndDate = null,
                },
            ],
            Leader = true,
            Contact = true,
        };

        _context.Contributors.Add(testContributor);
        await _context.SaveChangesAsync();

        ContributorRequestDTO updateDto = new()
        {
            Roles =
            [
                ContributorRoleType.Conceptualization,
                ContributorRoleType.Methodology,
            ],
            Position = ContributorPositionType.Consultant,
            Leader = false,
            Contact = false,
        };

        // Act
        ContributorResponseDTO updatedContributor =
            await contributorsService.UpdateContributorAsync(projectId, personId, updateDto);

        // Assert
        Assert.NotNull(updatedContributor);
        Assert.Equal(2, updatedContributor.Roles.Count);
        Assert.Equal(2, updatedContributor.Positions.Count);
        Assert.Contains(updatedContributor.Positions, p => p.Position == ContributorPositionType.CoInvestigator);
        Assert.Contains(updatedContributor.Positions, p => p.Position == ContributorPositionType.Consultant);
        Assert.Contains(updatedContributor.Roles, r => r.RoleType == ContributorRoleType.Conceptualization);
        Assert.Contains(updatedContributor.Roles, r => r.RoleType == ContributorRoleType.Methodology);
    }
}
