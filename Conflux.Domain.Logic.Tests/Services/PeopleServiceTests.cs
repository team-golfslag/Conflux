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

        // Add project to the context
        Guid projectId = Guid.NewGuid();
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
            Description = "Test Description",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(1),
        };
        _context.Projects.Add(project);

        // Add person to the context
        Guid personId = Guid.NewGuid();
        Person person = new()
        {
            Id = personId,
            Name = "John Doe",
        };
        _context.People.Add(person);

        ContributorDTO dto = new()
        {
            Leader = true,
            Contact = true,
        };

        // Act
        Contributor contributor = await contributorsService.CreateContributorAsync(projectId, dto);

        // Assert
        Assert.NotNull(contributor);
        Assert.Single(await _context.Contributors
            .Where(p => p.PersonId == contributor.PersonId && p.ProjectId == contributor.ProjectId).ToListAsync());
    }

    [Fact]
    public async Task GetContributorById_ShouldReturnContributor_WhenContributorExists()
    {
        // Arrange
        ContributorsService contributorsService = new(_context);

        // Add project to the context
        Guid projectId = Guid.NewGuid();
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
            Description = "Test Description",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(1),
        };
        _context.Projects.Add(project);

        // Add person to the context
        Guid personId = Guid.NewGuid();
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
        Contributor contributor = await contributorsService.GetContributorByIdAsync(projectId, personId);

        // Assert
        Assert.NotNull(contributor);
        Assert.Equal(testContributor.PersonId, contributor.PersonId);
    }

    [Fact]
    public async Task GetContributorById_ShouldReturnNotFound_WhenContributorDoesNotExist()
    {
        // Arrange
        ContributorsService contributorService = new(_context);

        Guid projectId = Guid.NewGuid();
        Guid personId = Guid.NewGuid();

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
        Guid projectId = Guid.NewGuid();
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
            Description = "Test Description",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(1),
        };
        _context.Projects.Add(project);

        // Add person to the context
        Guid personId = Guid.NewGuid();
        Person person = new()
        {
            Id = personId,
            Name = "John Doe",
        };
        _context.People.Add(person);

        ContributorDTO dto = new()
        {
            Leader = true,
            Contact = true,
        };

        // Act
        Contributor contributor = await contributorsService.CreateContributorAsync(projectId, dto);
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
        Guid projectId = Guid.NewGuid();
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
            Description = "Test Description",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(1),
        };
        _context.Projects.Add(project);

        // Add person to the context
        Guid personId = Guid.NewGuid();
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
                    EndDate = DateTime.UtcNow.AddMonths(1),
                },
            ],
            Leader = true,
            Contact = true,
        };

        _context.Contributors.Add(testContributor);
        await _context.SaveChangesAsync();

        ContributorDTO updateDto = new()
        {
            Roles =
            [
                ContributorRoleType.Conceptualization,
                ContributorRoleType.Methodology,
            ],
            Positions =
            [
                new()
                {
                    StartDate = DateTime.UtcNow,
                    Type = ContributorPositionType.Consultant,
                },
            ],
            Leader = false,
            Contact = false,
        };

        // Act
        Contributor updatedContributor =
            await contributorsService.UpdateContributorAsync(projectId, personId, updateDto);

        // Assert
        Assert.NotNull(updatedContributor);
        Assert.Equal(2, updatedContributor.Roles.Count);
        Assert.Single(updatedContributor.Positions);
        Assert.Equal(ContributorPositionType.Consultant, updatedContributor.Positions[0].Position);
        Assert.Contains(updatedContributor.Roles, r => r.RoleType == ContributorRoleType.Conceptualization);
        Assert.Contains(updatedContributor.Roles, r => r.RoleType == ContributorRoleType.Methodology);
    }
}
