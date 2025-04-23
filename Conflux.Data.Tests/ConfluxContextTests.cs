// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

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
}
