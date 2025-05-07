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
using Testcontainers.PostgreSql;
using Xunit;

namespace Conflux.Domain.Logic.Tests.Services;

public class ContributorsServiceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();
    private ConfluxContext _context = null!;

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
            Person = person,
            ProjectId = projectId,
            Leader = true,
            Contact = true,
        };

        // Act
        ContributorDTO contributor = await contributorsService.CreateContributorAsync(dto);

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
        ContributorDTO contributor = await contributorsService.GetContributorByIdAsync(projectId, personId);

        // Assert
        Assert.NotNull(contributor);
        Assert.Equal(testContributor.PersonId, contributor.Person?.Id);
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
            Person = person,
            ProjectId = projectId,
            Leader = true,
            Contact = true,
        };

        // Act
        ContributorDTO contributor = await contributorsService.CreateContributorAsync(dto);
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
            Person = new()
            {
                Id = personId,
                Name = "Jane Doe",
            },
            ProjectId = projectId,
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
        ContributorDTO updatedContributor =
            await contributorsService.UpdateContributorAsync(projectId, personId, updateDto);

        // Assert
        Assert.NotNull(updatedContributor);
        Assert.Equal(2, updatedContributor.Roles.Count);
        Assert.Single(updatedContributor.Positions);
        Assert.Equal(ContributorPositionType.Consultant, updatedContributor.Positions[0].Type);
        Assert.Contains(updatedContributor.Roles, r => r == ContributorRoleType.Conceptualization);
        Assert.Contains(updatedContributor.Roles, r => r == ContributorRoleType.Methodology);
    }

    [Fact]
    public async Task PatchContributorAsync_UpdatesRolesOnly_WhenRolesProvided()
    {
        // Arrange
        Contributor contributor = await SeedContributor();
        ContributorsService contributorService = new(_context);

        ContributorPatchDTO patchDto = new()
        {
            Roles = [ContributorRoleType.Software, ContributorRoleType.Validation],
        };

        // Act
        ContributorDTO result =
            await contributorService.PatchContributorAsync(contributor.ProjectId, contributor.PersonId, patchDto);

        // Assert
        Assert.Equal(2, result.Roles.Count);
        Assert.Contains(result.Roles, r => r == ContributorRoleType.Software);
        Assert.Contains(result.Roles, r => r == ContributorRoleType.Validation);

        // Ensure other properties weren't changed
        Assert.Equal(contributor.Leader, result.Leader);
        Assert.Equal(contributor.Contact, result.Contact);
        Assert.Equal(contributor.Positions.Count, result.Positions.Count);

        // Verify changes persisted to database
        Contributor dbContributor = await _context.Contributors
            .Include(c => c.Roles)
            .SingleAsync(c => c.ProjectId == contributor.ProjectId && c.PersonId == contributor.PersonId);
        Assert.Equal(2, dbContributor.Roles.Count);
    }

    [Fact]
    public async Task PatchContributorAsync_UpdatesPositionsOnly_WhenPositionsProvided()
    {
        // Arrange
        Contributor contributor = await SeedContributor();
        ContributorsService contributorService = new(_context);

        ContributorPatchDTO patchDto = new()
        {
            Positions =
            [
                new ContributorPositionRequestDTO
                {
                    Type = ContributorPositionType.Partner,
                    StartDate = new(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                },
            ],
        };

        // Act
        ContributorDTO result =
            await contributorService.PatchContributorAsync(contributor.ProjectId, contributor.PersonId, patchDto);

        // Assert
        Assert.Single(result.Positions);
        Assert.Equal(ContributorPositionType.Partner, result.Positions[0].Type);
        Assert.Equal(new(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc), result.Positions[0].StartDate);

        // Ensure other properties weren't changed
        Assert.Equal(contributor.Leader, result.Leader);
        Assert.Equal(contributor.Contact, result.Contact);
        Assert.Equal(contributor.Roles.Count, result.Roles.Count);

        // Verify changes persisted to database
        Contributor dbContributor = await _context.Contributors
            .Include(c => c.Positions)
            .SingleAsync(c => c.ProjectId == contributor.ProjectId && c.PersonId == contributor.PersonId);
        Assert.Single(dbContributor.Positions);
    }

    [Fact]
    public async Task PatchContributorAsync_UpdatesLeaderOnly_WhenLeaderProvided()
    {
        // Arrange
        Contributor contributor = await SeedContributor();
        bool originalLeader = contributor.Leader;
        ContributorsService contributorService = new(_context);

        ContributorPatchDTO patchDto = new()
        {
            Leader = !originalLeader,
        };

        // Act
        ContributorDTO result =
            await contributorService.PatchContributorAsync(contributor.ProjectId, contributor.PersonId, patchDto);

        // Assert
        Assert.NotEqual(originalLeader, result.Leader);

        // Ensure other properties weren't changed
        Assert.Equal(contributor.Contact, result.Contact);
        Assert.Equal(contributor.Roles.Count, result.Roles.Count);
        Assert.Equal(contributor.Positions.Count, result.Positions.Count);

        // Verify changes persisted to database
        Contributor dbContributor = await _context.Contributors
            .SingleAsync(c => c.ProjectId == contributor.ProjectId && c.PersonId == contributor.PersonId);
        Assert.NotEqual(originalLeader, dbContributor.Leader);
    }

    [Fact]
    public async Task PatchContributorAsync_UpdatesContactOnly_WhenContactProvided()
    {
        // Arrange
        Contributor contributor = await SeedContributor();
        bool originalContact = contributor.Contact;
        ContributorsService contributorService = new(_context);

        ContributorPatchDTO patchDto = new()
        {
            Contact = !originalContact,
        };

        // Act
        ContributorDTO result =
            await contributorService.PatchContributorAsync(contributor.ProjectId, contributor.PersonId, patchDto);

        // Assert
        Assert.NotEqual(originalContact, result.Contact);

        // Ensure other properties weren't changed
        Assert.Equal(contributor.Leader, result.Leader);
        Assert.Equal(contributor.Roles.Count, result.Roles.Count);
        Assert.Equal(contributor.Positions.Count, result.Positions.Count);

        // Verify changes persisted to database
        Contributor dbContributor = await _context.Contributors
            .SingleAsync(c => c.ProjectId == contributor.ProjectId && c.PersonId == contributor.PersonId);
        Assert.NotEqual(originalContact, dbContributor.Contact);
    }

    [Fact]
    public async Task PatchContributorAsync_UpdatesAllProperties_WhenAllPropertiesProvided()
    {
        // Arrange
        Contributor contributor = await SeedContributor();
        ContributorsService contributorService = new(_context);

        ContributorPatchDTO patchDto = new()
        {
            Roles = [ContributorRoleType.ProjectAdministration],
            Positions =
            [
                new ContributorPositionRequestDTO
                {
                    Type = ContributorPositionType.PrincipalInvestigator,
                    StartDate = new(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                },
            ],
            Leader = true,
            Contact = true,
        };

        // Act
        ContributorDTO result =
            await contributorService.PatchContributorAsync(contributor.ProjectId, contributor.PersonId, patchDto);

        // Assert
        Assert.Single(result.Roles);
        Assert.Equal(ContributorRoleType.ProjectAdministration, result.Roles[0]);

        Assert.Single(result.Positions);
        Assert.Equal(ContributorPositionType.PrincipalInvestigator, result.Positions[0].Type);

        Assert.True(result.Leader);
        Assert.True(result.Contact);

        // Verify changes persisted to database
        Contributor dbContributor = await _context.Contributors
            .Include(c => c.Roles)
            .Include(c => c.Positions)
            .SingleAsync(c => c.ProjectId == contributor.ProjectId && c.PersonId == contributor.PersonId);

        Assert.Single(dbContributor.Roles);
        Assert.Single(dbContributor.Positions);
        Assert.True(dbContributor.Leader);
        Assert.True(dbContributor.Contact);
    }

    [Fact]
    public async Task PatchContributorAsync_ThrowsException_WhenContributorNotFound()
    {
        // Arrange
        ContributorsService contributorService = new(_context);
        Guid nonExistentProjectId = Guid.NewGuid();
        Guid nonExistentPersonId = Guid.NewGuid();

        ContributorPatchDTO patchDto = new()
        {
            Leader = true,
        };

        // Act & Assert
        await Assert.ThrowsAsync<ContributorNotFoundException>(() =>
            contributorService.PatchContributorAsync(nonExistentProjectId, nonExistentPersonId, patchDto));
    }

    // Helper method to seed a contributor for testing
    private async Task<Contributor> SeedContributor()
    {
        // Create project
        Project project = new()
        {
            Id = Guid.NewGuid(),
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
        };
        await _context.Projects.AddAsync(project);

        // Create person
        Person person = new()
        {
            Id = Guid.NewGuid(),
            Name = "Test Person",
        };
        await _context.People.AddAsync(person);

        // Create contributor
        Contributor contributor = new()
        {
            ProjectId = project.Id,
            PersonId = person.Id,
            Leader = false,
            Contact = false,
            Roles =
            [
                new ContributorRole
                {
                    ProjectId = project.Id,
                    PersonId = person.Id,
                    RoleType = ContributorRoleType.Investigation,
                },
            ],
            Positions =
            [
                new ContributorPosition
                {
                    ProjectId = project.Id,
                    PersonId = person.Id,
                    Position = ContributorPositionType.Consultant,
                    StartDate = DateTime.UtcNow,
                },
            ],
        };
        await _context.Contributors.AddAsync(contributor);
        await _context.SaveChangesAsync();

        return contributor;
    }
}
