// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain.Logic.Services;
using Moq;
using Moq.Protected;
using SURFSharekit.Net.Models.RepoItem;
using Xunit;

namespace Conflux.Domain.Logic.Tests.Services;

public class SURFSharekitServiceTests
{
    Mock<ISURFSharekitService> _surfSharekitService;

    public SURFSharekitServiceTests()
    {
        _surfSharekitService = new Mock<ISURFSharekitService>();
    }


    [Fact]
    public void ProcessRepoItem_ShouldThrow_WhenPayloadFormatIsInvalid()
    {
        // Arrange
        // Act
        // Assert
    }

    [Fact]
    public void ProcessRepoItem_ShouldReturnNull_WhenNoRaid()
    {
        // Arrange
        // Act
        // Assert
    }

    [Fact]
    public void ProcessRepoItem_ShouldReturnNull_WhenNoId()
    {
        // Arrange
        // Act
        // Assert
    }

    [Fact]
    public void ProcessRepoItem_ShouldReturnNull_WhenNoAttributes()
    {
        // Arrange
        // Act
        // Assert
    }
}
