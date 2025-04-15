// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.RepositoryConnections.NWOpen;
using Xunit;

namespace Conflux.RepositoryConnections.Tests;

public class TemporaryProjectRetrieverTests
{
    /// <summary>
    /// Given a call to GetInstance(),
    /// When called multiple times,
    /// Then the same instance is returned each time.
    /// </summary>
    [Fact]
    public void GetInstance_ReturnsSameSingletonInstance()
    {
        // Arrange & Act
        TemporaryProjectRetriever retriever1 = TemporaryProjectRetriever.GetInstance();
        TemporaryProjectRetriever retriever2 = TemporaryProjectRetriever.GetInstance();

        // Assert
        Assert.Same(retriever1, retriever2);
    }

    /// <summary>
    /// Given multiple concurrent calls to GetInstance(),
    /// When called multiple times,
    /// Then the same instance is returned each time.
    /// </summary>
    [Fact]
    public void GetInstance_ConcurrentCalls_ReturnSameInstance()
    {
        TemporaryProjectRetriever[] instances = new TemporaryProjectRetriever[10];

        Parallel.For(0, 10, i => { instances[i] = TemporaryProjectRetriever.GetInstance(); });

        TemporaryProjectRetriever first = instances[0];
        Assert.All(instances, instance => Assert.Same(first, instance));
    }

    /// <summary>
    /// Given a call to MapProjectsAsync,
    /// When called with a valid count,
    /// Then a SeedData object is returned.
    /// </summary>
    [Fact]
    public async Task MapProjectsAsync_ReturnsNonNullSeedData()
    {
        // Arrange
        TemporaryProjectRetriever retriever = TemporaryProjectRetriever.GetInstance();

        // Act
        SeedData seedData = await retriever.MapProjectsAsync(5);

        // Assert
        Assert.NotNull(seedData);
        Assert.NotNull(seedData.Projects);
        Assert.NotNull(seedData.Products);
        Assert.NotNull(seedData.Contributors);
        Assert.NotNull(seedData.Parties);
    }

    /// <summary>
    /// Given multiple calls to MapProjectsAsync,
    /// When called multiple times,
    /// Then the seed data is consistent.
    /// </summary>
    [Fact]
    public async Task MapProjectsAsync_MultipleCalls_ReturnsConsistentSeedData()
    {
        // Arrange
        TemporaryProjectRetriever retriever = TemporaryProjectRetriever.GetInstance();

        // Act
        SeedData firstResult = await retriever.MapProjectsAsync(10);
        SeedData secondResult = await retriever.MapProjectsAsync(10);

        // Assert
        Assert.NotNull(firstResult);
        Assert.NotNull(secondResult);

        Assert.NotNull(firstResult.Projects);
        Assert.NotNull(secondResult.Projects);
    }
}
