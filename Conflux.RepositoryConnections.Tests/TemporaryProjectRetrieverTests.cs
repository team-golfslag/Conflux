using Conflux.RepositoryConnections.NWOpen;
using Xunit;

namespace RepositoryConnections.Tests;

public class TemporaryProjectRetrieverTests
{
    [Fact]
    public void Test()
    {
        TemporaryProjectRetriever mapper = TemporaryProjectRetriever.GetInstance();

        // Act
        var result = mapper.MapProjectsAsync();

        // Assert
        Assert.NotNull(result);
    }
}
