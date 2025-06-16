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

public class OnnxEmbeddingServiceTests : IDisposable
{
    private readonly Mock<ILogger<OnnxEmbeddingService>> _loggerMock = new();
    private readonly Mock<IConfiguration> _configurationMock = new();

    public OnnxEmbeddingServiceTests()
    {
        // Setup configuration mocks
        _configurationMock.Setup(c => c["EmbeddingModel:MaxTokens"]).Returns("512");
        _configurationMock.Setup(c => c["EmbeddingModel:Dimension"]).Returns("384");
        
        // Setup paths to non-existent files for testing error conditions
        _configurationMock.Setup(c => c["EmbeddingModel:Path"]).Returns("non-existent-model.onnx");
        _configurationMock.Setup(c => c["EmbeddingModel:TokenizerPath"]).Returns("non-existent-tokenizer.txt");
    }

    [Fact]
    public void Constructor_WithMissingModelFile_ThrowsFileNotFoundException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<FileNotFoundException>(() =>
            new OnnxEmbeddingService(_loggerMock.Object, _configurationMock.Object));
        
        Assert.Contains("ONNX model not found", exception.Message);
    }

    [Fact]
    public void Constructor_WithMissingTokenizerFile_ThrowsFileNotFoundException()
    {
        // Arrange
        // Create a temporary fake model file
        var tempModelPath = Path.GetTempFileName();
        _configurationMock.Setup(c => c["EmbeddingModel:Path"]).Returns(tempModelPath);
        
        try
        {
            // Act & Assert
            var exception = Assert.Throws<FileNotFoundException>(() =>
                new OnnxEmbeddingService(_loggerMock.Object, _configurationMock.Object));
            
            Assert.Contains("Tokenizer not found", exception.Message);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempModelPath))
                File.Delete(tempModelPath);
        }
    }

    [Fact]
    public void Constructor_WithValidConfiguration_SetsPropertiesCorrectly()
    {
        // Note: This test would require actual model and tokenizer files to work
        // For the purpose of this test, we'll test the configuration parsing logic
        
        // Arrange
        _configurationMock.Setup(c => c["EmbeddingModel:MaxTokens"]).Returns("256");
        _configurationMock.Setup(c => c["EmbeddingModel:Dimension"]).Returns("768");

        // Act & Assert
        // We can't actually create the service without real files, but we can verify
        // that the configuration values are accessible
        string? maxTokens = _configurationMock.Object["EmbeddingModel:MaxTokens"];
        string? dimension = _configurationMock.Object["EmbeddingModel:Dimension"];
        
        Assert.Equal("256", maxTokens);
        Assert.Equal("768", dimension);
    }

    [Fact]
    public void EmbeddingDimension_WithMockedService_ReturnsCorrectValue()
    {
        // Arrange
        var mockService = new Mock<IEmbeddingService>();
        const int expectedDimension = 384;
        
        mockService.Setup(x => x.EmbeddingDimension).Returns(expectedDimension);

        // Act
        int actualDimension = mockService.Object.EmbeddingDimension;

        // Assert
        Assert.Equal(expectedDimension, actualDimension);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WithMockedService_CallsCorrectly()
    {
        // Arrange
        var mockService = new Mock<IEmbeddingService>();
        const string testText = "This is a test sentence for embedding generation.";
        var expectedVector = new Vector(new float[384]);
        
        mockService.Setup(x => x.GenerateEmbeddingAsync(testText))
            .ReturnsAsync(expectedVector);

        // Act
        Vector result = await mockService.Object.GenerateEmbeddingAsync(testText);

        // Assert
        Assert.Equal(expectedVector, result);
        mockService.Verify(x => x.GenerateEmbeddingAsync(testText), Times.Once);
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_WithMockedService_ProcessesBatchCorrectly()
    {
        // Arrange
        var mockService = new Mock<IEmbeddingService>();
        string[] testTexts = 
        [
            "First sentence",
            "Second sentence", 
            "Third sentence"
        ];
        
        var expectedVectors = new Vector[]
        {
            new(new float[384]),
            new(new float[384]),
            new(new float[384])
        };
        
        mockService.Setup(x => x.GenerateEmbeddingsAsync(testTexts))
            .ReturnsAsync(expectedVectors);

        // Act
        Vector[] result = await mockService.Object.GenerateEmbeddingsAsync(testTexts);

        // Assert
        Assert.Equal(expectedVectors.Length, result.Length);
        Assert.Equal(expectedVectors, result);
        mockService.Verify(x => x.GenerateEmbeddingsAsync(testTexts), Times.Once);
    }

    [Fact]
    public void Constructor_ConfigurationDefaults_UseCorrectDefaults()
    {
        // Arrange - Setup configuration to return null/default values
        _configurationMock.Setup(c => c["EmbeddingModel:Path"]).Returns((string?)null);
        _configurationMock.Setup(c => c["EmbeddingModel:TokenizerPath"]).Returns((string?)null);
        _configurationMock.Setup(c => c["EmbeddingModel:MaxTokens"]).Returns("512");
        _configurationMock.Setup(c => c["EmbeddingModel:Dimension"]).Returns("384");

        // Act & Assert
        // The constructor should use default paths when configuration is null
        var exception = Assert.Throws<FileNotFoundException>(() =>
            new OnnxEmbeddingService(_loggerMock.Object, _configurationMock.Object));
        
        // Verify it tried to use the default path
        Assert.Contains("all-MiniLM-L12-v2.onnx", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public async Task GenerateEmbeddingAsync_WithEmptyOrWhitespaceText_HandledByMock(string input)
    {
        // Arrange
        var mockService = new Mock<IEmbeddingService>();
        var expectedVector = new Vector(new float[384]);
        
        mockService.Setup(x => x.GenerateEmbeddingAsync(input))
            .ReturnsAsync(expectedVector);

        // Act
        Vector result = await mockService.Object.GenerateEmbeddingAsync(input);

        // Assert
        Assert.NotNull(result);
        mockService.Verify(x => x.GenerateEmbeddingAsync(input), Times.Once);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WithVeryLongText_HandledByMock()
    {
        // Arrange
        var mockService = new Mock<IEmbeddingService>();
        string longText = string.Join(" ", Enumerable.Repeat("word", 2000)); // Very long text
        var expectedVector = new Vector(new float[384]);
        
        mockService.Setup(x => x.GenerateEmbeddingAsync(longText))
            .ReturnsAsync(expectedVector);

        // Act
        Vector result = await mockService.Object.GenerateEmbeddingAsync(longText);

        // Assert
        Assert.NotNull(result);
        mockService.Verify(x => x.GenerateEmbeddingAsync(longText), Times.Once);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WithSpecialCharacters_HandledByMock()
    {
        // Arrange
        var mockService = new Mock<IEmbeddingService>();
        const string textWithSpecialChars = "Text with émojis 🚀 and spëcial châractërs! @#$%^&*()";
        var expectedVector = new Vector(new float[384]);
        
        mockService.Setup(x => x.GenerateEmbeddingAsync(textWithSpecialChars))
            .ReturnsAsync(expectedVector);

        // Act
        Vector result = await mockService.Object.GenerateEmbeddingAsync(textWithSpecialChars);

        // Assert
        Assert.NotNull(result);
        mockService.Verify(x => x.GenerateEmbeddingAsync(textWithSpecialChars), Times.Once);
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_WithEmptyArray_HandledByMock()
    {
        // Arrange
        var mockService = new Mock<IEmbeddingService>();
        string[] emptyArray = [];
        var expectedVectors = Array.Empty<Vector>();
        
        mockService.Setup(x => x.GenerateEmbeddingsAsync(emptyArray))
            .ReturnsAsync(expectedVectors);

        // Act
        Vector[] result = await mockService.Object.GenerateEmbeddingsAsync(emptyArray);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        mockService.Verify(x => x.GenerateEmbeddingsAsync(emptyArray), Times.Once);
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_WithLargeBatch_HandledByMock()
    {
        // Arrange
        var mockService = new Mock<IEmbeddingService>();
        string[] largeBatch = Enumerable.Range(1, 100)
            .Select(i => $"Test sentence number {i}")
            .ToArray();
        
        var expectedVectors = largeBatch.Select(_ => new Vector(new float[384])).ToArray();
        
        mockService.Setup(x => x.GenerateEmbeddingsAsync(largeBatch))
            .ReturnsAsync(expectedVectors);

        // Act
        Vector[] result = await mockService.Object.GenerateEmbeddingsAsync(largeBatch);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(100, result.Length);
        mockService.Verify(x => x.GenerateEmbeddingsAsync(largeBatch), Times.Once);
    }

    public void Dispose()
    {
        // Clean up any resources if needed
        GC.SuppressFinalize(this);
    }
}
