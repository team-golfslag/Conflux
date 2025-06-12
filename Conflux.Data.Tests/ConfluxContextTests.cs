// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Microsoft.EntityFrameworkCore;

namespace Conflux.Data.Tests;

public class ConfluxContextTests
{
    /// <summary>
    /// Given a database context
    /// When the context is created
    /// Then the context should not be null
    /// </summary>
    [Fact]
    public async Task Can_Create_ConfluxContext()
    {
        DbContextOptions<ConfluxContext> options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase(Guid.CreateVersion7().ToString())
            .Options;
        
        ConfluxContext context = new(options);

        // Assert
        Assert.NotNull(context);
    }
}
