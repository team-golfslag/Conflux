// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.API.Attributes;
using Xunit;

namespace Conflux.API.Tests.Attributes;

public class RouteParamNameAttributeTests
{
    [Fact]
    public void Constructor_SetsName()
    {
        // Arrange & Act
        RouteParamNameAttribute attribute = new("test");

        // Assert
        Assert.Equal("test", attribute.Name);
    }

    [Theory]
    [InlineData("id")]
    [InlineData("projectId")]
    [InlineData("userId")]
    public void Constructor_WithDifferentNames_SetsNameProperty(string name)
    {
        // Arrange & Act
        RouteParamNameAttribute attribute = new(name);

        // Assert
        Assert.Equal(name, attribute.Name);
    }
}
