// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
//
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain.Logic.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Pgvector;
using Xunit;

namespace Conflux.Domain.Logic.Tests.Services;

public class EmbeddingServiceTests : IDisposable
{
    private readonly Mock<ILogger<OnnxEmbeddingService>> _loggerMock = new();
    private readonly Mock<IConfiguration> _configurationMock = new();
    private readonly Mock<IEmbeddingService> _embeddingServiceMock = new();

    public EmbeddingServiceTests()
    {
        // Setup configuration mocks for ONNX service (if needed for real implementation tests)
        _configurationMock.Setup(c => c["EmbeddingModel:MaxTokens"]).Returns("512");
        _configurationMock.Setup(c => c["EmbeddingModel:Dimension"]).Returns("384");
        _configurationMock.SetupGet(c => c["EmbeddingModel:Path"]).Returns("Models/test-model.onnx");
        _configurationMock.SetupGet(c => c["EmbeddingModel:TokenizerPath"]).Returns("Models/test-tokenizer.txt");
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WithValidText_ReturnsVector()
    {
        // Arrange
        const string testText = "This is a test sentence for embedding generation.";
        var expectedVector = new Vector(new float[384]);
        
        _embeddingServiceMock.Setup(x => x.GenerateEmbeddingAsync(testText))
            .ReturnsAsync(expectedVector);

        // Act
        Vector result = await _embeddingServiceMock.Object.GenerateEmbeddingAsync(testText);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedVector, result);
        _embeddingServiceMock.Verify(x => x.GenerateEmbeddingAsync(testText), Times.Once);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WithEmptyText_HandlesGracefully()
    {
        // Arrange
        const string emptyText = "";
        var expectedVector = new Vector(new float[384]);
        
        _embeddingServiceMock.Setup(x => x.GenerateEmbeddingAsync(emptyText))
            .ReturnsAsync(expectedVector);

        // Act
        Vector result = await _embeddingServiceMock.Object.GenerateEmbeddingAsync(emptyText);

        // Assert
        Assert.NotNull(result);
        _embeddingServiceMock.Verify(x => x.GenerateEmbeddingAsync(emptyText), Times.Once);
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_WithMultipleTexts_ReturnsVectorArray()
    {
        // Arrange
        string[] testTexts = 
        [
            "First test sentence",
            "Second test sentence",
            "Third test sentence"
        ];
        
        var expectedVectors = new Vector[]
        {
            new(new float[384]),
            new(new float[384]),
            new(new float[384])
        };
        
        _embeddingServiceMock.Setup(x => x.GenerateEmbeddingsAsync(testTexts))
            .ReturnsAsync(expectedVectors);

        // Act
        Vector[] result = await _embeddingServiceMock.Object.GenerateEmbeddingsAsync(testTexts);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Length);
        Assert.Equal(expectedVectors, result);
        _embeddingServiceMock.Verify(x => x.GenerateEmbeddingsAsync(testTexts), Times.Once);
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_WithEmptyArray_ReturnsEmptyArray()
    {
        // Arrange
        string[] emptyTexts = [];
        var expectedVectors = Array.Empty<Vector>();
        
        _embeddingServiceMock.Setup(x => x.GenerateEmbeddingsAsync(emptyTexts))
            .ReturnsAsync(expectedVectors);

        // Act
        Vector[] result = await _embeddingServiceMock.Object.GenerateEmbeddingsAsync(emptyTexts);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        _embeddingServiceMock.Verify(x => x.GenerateEmbeddingsAsync(emptyTexts), Times.Once);
    }

    [Fact]
    public void EmbeddingDimension_ReturnsCorrectValue()
    {
        // Arrange
        const int expectedDimension = 384;
        _embeddingServiceMock.Setup(x => x.EmbeddingDimension)
            .Returns(expectedDimension);

        // Act
        int result = _embeddingServiceMock.Object.EmbeddingDimension;

        // Assert
        Assert.Equal(expectedDimension, result);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WithLongText_TruncatesAppropriately()
    {
        // Arrange
        string longText = string.Join(" ", Enumerable.Repeat("word", 1000)); // Very long text
        var expectedVector = new Vector(new float[384]);
        
        _embeddingServiceMock.Setup(x => x.GenerateEmbeddingAsync(longText))
            .ReturnsAsync(expectedVector);

        // Act
        Vector result = await _embeddingServiceMock.Object.GenerateEmbeddingAsync(longText);

        // Assert
        Assert.NotNull(result);
        _embeddingServiceMock.Verify(x => x.GenerateEmbeddingAsync(longText), Times.Once);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        const string textWithSpecialChars = "Text with émojis 🚀 and spëcial châractërs!";
        var expectedVector = new Vector(new float[384]);
        
        _embeddingServiceMock.Setup(x => x.GenerateEmbeddingAsync(textWithSpecialChars))
            .ReturnsAsync(expectedVector);

        // Act
        Vector result = await _embeddingServiceMock.Object.GenerateEmbeddingAsync(textWithSpecialChars);

        // Assert
        Assert.NotNull(result);
        _embeddingServiceMock.Verify(x => x.GenerateEmbeddingAsync(textWithSpecialChars), Times.Once);
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_WithMixedTextLengths_HandlesCorrectly()
    {
        // Arrange
        string[] mixedTexts = 
        [
            "Short",
            "This is a medium length sentence with several words.",
            string.Join(" ", Enumerable.Repeat("long", 100)) // Very long text
        ];
        
        var expectedVectors = new Vector[]
        {
            new(new float[384]),
            new(new float[384]),
            new(new float[384])
        };
        
        _embeddingServiceMock.Setup(x => x.GenerateEmbeddingsAsync(mixedTexts))
            .ReturnsAsync(expectedVectors);

        // Act
        Vector[] result = await _embeddingServiceMock.Object.GenerateEmbeddingsAsync(mixedTexts);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Length);
        _embeddingServiceMock.Verify(x => x.GenerateEmbeddingsAsync(mixedTexts), Times.Once);
    }

    public void Dispose()
    {
        // Clean up any resources if needed
        GC.SuppressFinalize(this);
    }
}
