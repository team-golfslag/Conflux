// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Moq;
using NWOpen.Net;
using NWOpen.Net.Models;
using NWOpen.Net.Services;
using Xunit;

namespace Conflux.Integrations.NWOpen.Tests;

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
        Assert.Empty(seedData.Organisations);
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
                ApiType = null!,
                Version = null!,
                Funder = null!,
                RorId = null!,
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
        Assert.NotNull(seedData.Organisations);
    }
}
