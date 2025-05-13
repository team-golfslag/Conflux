// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Text.Json;
using Conflux.Domain.Logic.Extensions;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Conflux.Domain.Logic.Tests.Extensions;

public class ISessionExtensionsTests
{
    [Fact]
    public void Set_WithSimpleValue_StoresSerializedData()
    {
        // Arrange
        var mockSession = new Mock<ISession>();
        byte[] storedData = null!;
        mockSession.Setup(s => s.Set(It.IsAny<string>(), It.IsAny<byte[]>()))
            .Callback<string, byte[]>((_, value) => storedData = value);

        // Act
        mockSession.Object.Set("testKey", "testValue");

        // Assert
        Assert.NotNull(storedData);
        byte[] expectedData = JsonSerializer.SerializeToUtf8Bytes("testValue");
        Assert.Equal(expectedData, storedData);
    }

    [Fact]
    public void Set_WithComplexObject_StoresSerializedData()
    {
        // Arrange
        var mockSession = new Mock<ISession>();
        byte[] storedData = null!;
        mockSession.Setup(s => s.Set(It.IsAny<string>(), It.IsAny<byte[]>()))
            .Callback<string, byte[]>((_, value) => storedData = value);
        TestClass testObject = new()
        {
            Id = 1,
            Name = "Test",
        };

        // Act
        mockSession.Object.Set("testKey", testObject);

        // Assert
        Assert.NotNull(storedData);
        byte[] expectedData = JsonSerializer.SerializeToUtf8Bytes(testObject);
        Assert.Equal(expectedData, storedData);
    }

    [Fact]
    public void Set_WithNullValue_StoresSerializedNull()
    {
        // Arrange
        var mockSession = new Mock<ISession>();
        byte[] storedData = null!;
        mockSession.Setup(s => s.Set(It.IsAny<string>(), It.IsAny<byte[]>()))
            .Callback<string, byte[]>((_, value) => storedData = value);
        TestClass testObject = null!;

        // Act
        mockSession.Object.Set("testKey", testObject);

        // Assert
        Assert.NotNull(storedData);
        byte[] expectedData = JsonSerializer.SerializeToUtf8Bytes(testObject);
        Assert.Equal(expectedData, storedData);
    }

    [Fact]
    public void Get_WithExistingKey_ReturnsDeserializedValue()
    {
        // Arrange
        var mockSession = new Mock<ISession>();
        string testValue = "test value";
        byte[]? serializedValue = JsonSerializer.SerializeToUtf8Bytes(testValue);
        mockSession.Setup(s => s.TryGetValue("testKey", out serializedValue)).Returns(true);

        // Act
        string? result = mockSession.Object.Get<string>("testKey");

        // Assert
        Assert.Equal(testValue, result);
    }

    [Fact]
    public void Get_WithNonExistingKey_ReturnsDefault()
    {
        // Arrange
        var mockSession = new Mock<ISession>();
        byte[] outValue = null!;
        mockSession.Setup(s => s.TryGetValue("nonExistingKey", out outValue)).Returns(false);

        // Act
        string? result = mockSession.Object.Get<string>("nonExistingKey");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Get_WithComplexObject_ReturnsDeserializedObject()
    {
        // Arrange
        var mockSession = new Mock<ISession>();
        TestClass testObject = new()
        {
            Id = 42,
            Name = "Test Object",
        };
        byte[]? serializedValue = JsonSerializer.SerializeToUtf8Bytes(testObject);
        mockSession.Setup(s => s.TryGetValue("testKey", out serializedValue)).Returns(true);

        // Act
        TestClass? result = mockSession.Object.Get<TestClass>("testKey");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(42, result.Id);
        Assert.Equal("Test Object", result.Name);
    }

    [Fact]
    public void Get_WithEmptyByteArray_ReturnsDefaultValue()
    {
        // Arrange
        var mockSession = new Mock<ISession>();
        byte[]? emptyArray = [];
        mockSession.Setup(s => s.TryGetValue("emptyKey", out emptyArray)).Returns(true);

        // Act & Assert
        Assert.Throws<JsonException>(() => mockSession.Object.Get<string>("emptyKey"));
    }

    private class TestClass
    {
        public int Id { get; init; }
        public required string Name { get; init; }
    }
}
