using Conflux.RepositoryConnections.NWOpen;
using Moq;
using NWOpen.Net;
using NWOpen.Net.Models;
using NWOpen.Net.Services;
using Xunit;

namespace Conflux.RepositoryConnections.Tests;

public class TempProjectRetrieverServiceTests
{
    [Fact]
    public async Task MapProjectsAsync_NullResult_ReturnsEmptySeedData()
    {
        // Arrange: mock INWOpenService to return null result
        var queryBuilderMock = new Mock<INWOpenQueryBuilder>();
        queryBuilderMock
            .Setup(q => q.WithNumberOfResults(It.IsAny<int>()))
            .Returns(queryBuilderMock.Object);
        queryBuilderMock
            .Setup(q => q.ExecuteAsync())
            .ReturnsAsync((NWOpenResult?)null);

        var serviceMock = new Mock<INWOpenService>(MockBehavior.Strict);
        serviceMock
            .Setup(s => s.Query())
            .Returns(queryBuilderMock.Object);

        TempProjectRetrieverService retriever = new(serviceMock.Object);

        // Act
        SeedData seedData = await retriever.MapProjectsAsync(5);

        // Assert
        Assert.NotNull(seedData);
        Assert.Empty(seedData.Projects);
        Assert.Empty(seedData.Products);
        Assert.Empty(seedData.Contributors);
        Assert.Empty(seedData.Parties);
    }

    [Fact]
    public async Task MapProjectsAsync_WithProjects_ReturnsMappedSeedData()
    {
        // Arrange: mock INWOpenService to return a result with one project
        Project dummyProject = new()
        {
            ProjectId = "proj1",
            Title = "Test",
        };
        NWOpenResult result = new()
        {
            Metadata = new()
            {
                ApiType = null,
                Version = null,
                Funder = null,
                RorId = null,
            },
            Projects = [dummyProject],
        };

        var queryBuilderMock = new Mock<INWOpenQueryBuilder>();
        queryBuilderMock
            .Setup(q => q.WithNumberOfResults(10))
            .Returns(queryBuilderMock.Object);
        queryBuilderMock
            .Setup(q => q.ExecuteAsync())
            .ReturnsAsync(result);

        var serviceMock = new Mock<INWOpenService>(MockBehavior.Strict);
        serviceMock
            .Setup(s => s.Query())
            .Returns(queryBuilderMock.Object);

        TempProjectRetrieverService retriever = new(serviceMock.Object);

        // Act
        SeedData seedData = await retriever.MapProjectsAsync(10);

        // Assert
        Assert.NotNull(seedData);
        Assert.NotNull(seedData.Products);
        Assert.NotNull(seedData.Contributors);
        Assert.NotNull(seedData.Parties);
    }

    [Fact]
    public async Task MapProjectsAsync_ConcurrentCalls_ReturnsConsistentResults()
    {
        // Arrange: mock INWOpenService for concurrent scenario
        Project dummyProject = new()
        {
            ProjectId = "proj2",
            Title = "Concurrent",
        };
        NWOpenResult result = new()
        {
            Projects = [dummyProject],
            Metadata = null,
        };

        var queryBuilderMock = new Mock<INWOpenQueryBuilder>();
        queryBuilderMock
            .Setup(q => q.WithNumberOfResults(It.IsAny<int>()))
            .Returns(queryBuilderMock.Object);
        queryBuilderMock
            .Setup(q => q.ExecuteAsync())
            .ReturnsAsync(result);

        var serviceMock = new Mock<INWOpenService>(MockBehavior.Strict);
        serviceMock
            .Setup(s => s.Query())
            .Returns(queryBuilderMock.Object);

        TempProjectRetrieverService retriever = new(serviceMock.Object);

        // Act: make multiple concurrent calls
        var tasks = Enumerable.Range(0, 5)
            .Select(_ => retriever.MapProjectsAsync(1))
            .ToArray();
        await Task.WhenAll(tasks);

        // Assert: all calls yield the same mapped content
        foreach (var task in tasks)
        {
            SeedData res = await task;
            Assert.Equal(dummyProject.Title, res.Projects[0].Title);
        }
    }
}
