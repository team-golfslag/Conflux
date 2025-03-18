using Conflux.Domain;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Conflux.Data.Tests;

public class ConfluxContextTests
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();

    /// <summary>
    /// Given a database context
    /// When the context is created
    /// Then the context should not be null
    /// </summary>
    [Fact]
    public async Task Can_Create_ConfluxContext()
    {
        // Arrange & Act
        await _postgres.StartAsync();

        var options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseNpgsql(_postgres.GetConnectionString()).Options;

        ConfluxContext context = new(options);

        // Assert
        Assert.NotNull(context);
    }

    /// <summary>
    /// Given a database context
    /// When SeedDataAsync is called
    /// Then the database should be seeded with the correct data
    /// </summary>
    [Fact]
    public async Task SeedDataAsync_ShouldSeedDataCorrectly()
    {
        // Arrange
        await _postgres.StartAsync();

        var options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseNpgsql(_postgres.GetConnectionString()).Options;

        ConfluxContext context = new(options);
        await context.Database.EnsureCreatedAsync();

        // Act
        await context.SeedDataAsync();

        var projects = await context.Projects.ToListAsync();

        // Assertions
        Assert.NotEmpty(projects);

        Project project = projects[0];

        Assert.NotNull(project);
    }
}
