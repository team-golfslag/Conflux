// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
//
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain;
using Conflux.Domain.Logic.Extensions;
using Xunit;

namespace Conflux.Domain.Logic.Tests.Extensions;

public class ProjectEmbeddingExtensionsTests
{
    [Fact]
    public void GetEmbeddingText_WithPrimaryTitleAndDescription_ReturnsCorrectText()
    {
        // Arrange
        var project = new Project
        {
            Id = Guid.NewGuid(),
            StartDate = DateTime.UtcNow,
            SCIMId = "test-project",
            Titles = 
            [
                new ProjectTitle
                {
                    Id = Guid.NewGuid(),
                    Text = "Machine Learning Research",
                    Type = TitleType.Primary,
                    StartDate = DateTime.UtcNow,
                    Language = Language.ENGLISH
                }
            ],
            Descriptions = 
            [
                new ProjectDescription
                {
                    Id = Guid.NewGuid(),
                    Text = "Advanced research in machine learning algorithms.",
                    Type = DescriptionType.Primary,
                    Language = Language.ENGLISH
                }
            ]
        };

        // Act
        string result = project.GetEmbeddingText();

        // Assert
        Assert.Contains("Machine Learning Research", result);
        Assert.Contains("Advanced research in machine learning algorithms.", result);
    }

    [Fact]
    public void GetEmbeddingText_WithMultipleTitlesAndDescriptions_CombinesAllRelevantText()
    {
        // Arrange
        var project = new Project
        {
            Id = Guid.NewGuid(),
            StartDate = DateTime.UtcNow,
            SCIMId = "test-project",
            Titles = 
            [
                new ProjectTitle
                {
                    Id = Guid.NewGuid(),
                    Text = "Primary Title",
                    Type = TitleType.Primary,
                    StartDate = DateTime.UtcNow,
                    Language = Language.ENGLISH
                },
                new ProjectTitle
                {
                    Id = Guid.NewGuid(),
                    Text = "Alternative Title",
                    Type = TitleType.Alternative,
                    StartDate = DateTime.UtcNow,
                    Language = Language.ENGLISH
                }
            ],
            Descriptions = 
            [
                new ProjectDescription
                {
                    Id = Guid.NewGuid(),
                    Text = "Primary description text.",
                    Type = DescriptionType.Primary,
                    Language = Language.ENGLISH
                },
                new ProjectDescription
                {
                    Id = Guid.NewGuid(),
                    Text = "Brief description text.",
                    Type = DescriptionType.Brief,
                    Language = Language.ENGLISH
                },
                new ProjectDescription
                {
                    Id = Guid.NewGuid(),
                    Text = "Project objectives text.",
                    Type = DescriptionType.Objectives,
                    Language = Language.ENGLISH
                }
            ]
        };

        // Act
        string result = project.GetEmbeddingText();

        // Assert
        Assert.Contains("Primary Title", result);
        Assert.Contains("Alternative Title", result);
        Assert.Contains("Primary description text.", result);
        Assert.Contains("Brief description text.", result);
        Assert.Contains("Project objectives text.", result);
    }

    [Fact]
    public void GetEmbeddingText_WithEmptyProject_ReturnsEmptyString()
    {
        // Arrange
        var project = new Project
        {
            Id = Guid.NewGuid(),
            StartDate = DateTime.UtcNow,
            SCIMId = "empty-project",
            Titles = [],
            Descriptions = []
        };

        // Act
        string result = project.GetEmbeddingText();

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void GetEmbeddingText_WithNullOrWhitespaceText_IgnoresEmptyContent()
    {
        // Arrange
        var project = new Project
        {
            Id = Guid.NewGuid(),
            StartDate = DateTime.UtcNow,
            SCIMId = "test-project",
            Titles = 
            [
                new ProjectTitle
                {
                    Id = Guid.NewGuid(),
                    Text = "Valid Title",
                    Type = TitleType.Primary,
                    StartDate = DateTime.UtcNow,
                    Language = Language.ENGLISH
                },
                new ProjectTitle
                {
                    Id = Guid.NewGuid(),
                    Text = "",
                    Type = TitleType.Alternative,
                    StartDate = DateTime.UtcNow,
                    Language = Language.ENGLISH
                },
                new ProjectTitle
                {
                    Id = Guid.NewGuid(),
                    Text = "   ",
                    Type = TitleType.Alternative,
                    StartDate = DateTime.UtcNow,
                    Language = Language.ENGLISH
                }
            ],
            Descriptions = 
            [
                new ProjectDescription
                {
                    Id = Guid.NewGuid(),
                    Text = "Valid description.",
                    Type = DescriptionType.Primary,
                    Language = Language.ENGLISH
                },
                new ProjectDescription
                {
                    Id = Guid.NewGuid(),
                    Text = null!,
                    Type = DescriptionType.Brief,
                    Language = Language.ENGLISH
                }
            ]
        };

        // Act
        string result = project.GetEmbeddingText();

        // Assert
        Assert.Contains("Valid Title", result);
        Assert.Contains("Valid description.", result);
        Assert.DoesNotContain("   ", result.Trim());
        // Should only contain the non-empty content
        var parts = result.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        Assert.Contains("Valid", parts);
        Assert.Contains("Title", parts);
        Assert.Contains("Valid", parts);
        Assert.Contains("description.", parts);
    }

    [Fact]
    public void GetEmbeddingText_IgnoresNonRelevantDescriptionTypes()
    {
        // Arrange
        var project = new Project
        {
            Id = Guid.NewGuid(),
            StartDate = DateTime.UtcNow,
            SCIMId = "test-project",
            Titles = 
            [
                new ProjectTitle
                {
                    Id = Guid.NewGuid(),
                    Text = "Test Title",
                    Type = TitleType.Primary,
                    StartDate = DateTime.UtcNow,
                    Language = Language.ENGLISH
                }
            ],
            Descriptions = 
            [
                new ProjectDescription
                {
                    Id = Guid.NewGuid(),
                    Text = "Primary description - should be included.",
                    Type = DescriptionType.Primary,
                    Language = Language.ENGLISH
                },
                new ProjectDescription
                {
                    Id = Guid.NewGuid(),
                    Text = "Other description - should be excluded.",
                    Type = DescriptionType.Other,
                    Language = Language.ENGLISH
                }
            ]
        };

        // Act
        string result = project.GetEmbeddingText();

        // Assert
        Assert.Contains("Test Title", result);
        Assert.Contains("Primary description - should be included.", result);
        Assert.DoesNotContain("Other description - should be excluded.", result);
    }

    [Fact]
    public void GetEmbeddingContentHash_WithSameContent_ReturnsSameHash()
    {
        // Arrange
        var project1 = new Project
        {
            Id = Guid.NewGuid(),
            StartDate = DateTime.UtcNow,
            SCIMId = "test-project-1",
            Titles = 
            [
                new ProjectTitle
                {
                    Id = Guid.NewGuid(),
                    Text = "Identical Title",
                    Type = TitleType.Primary,
                    StartDate = DateTime.UtcNow,
                    Language = Language.ENGLISH
                }
            ],
            Descriptions = 
            [
                new ProjectDescription
                {
                    Id = Guid.NewGuid(),
                    Text = "Identical description.",
                    Type = DescriptionType.Primary,
                    Language = Language.ENGLISH
                }
            ]
        };

        var project2 = new Project
        {
            Id = Guid.NewGuid(),
            StartDate = DateTime.UtcNow,
            SCIMId = "test-project-2",
            Titles = 
            [
                new ProjectTitle
                {
                    Id = Guid.NewGuid(),
                    Text = "Identical Title",
                    Type = TitleType.Primary,
                    StartDate = DateTime.UtcNow,
                    Language = Language.ENGLISH
                }
            ],
            Descriptions = 
            [
                new ProjectDescription
                {
                    Id = Guid.NewGuid(),
                    Text = "Identical description.",
                    Type = DescriptionType.Primary,
                    Language = Language.ENGLISH
                }
            ]
        };

        // Act
        string hash1 = project1.GetEmbeddingContentHash();
        string hash2 = project2.GetEmbeddingContentHash();

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void GetEmbeddingContentHash_WithDifferentContent_ReturnsDifferentHash()
    {
        // Arrange
        var project1 = new Project
        {
            Id = Guid.NewGuid(),
            StartDate = DateTime.UtcNow,
            SCIMId = "test-project-1",
            Titles = 
            [
                new ProjectTitle
                {
                    Id = Guid.NewGuid(),
                    Text = "First Title",
                    Type = TitleType.Primary,
                    StartDate = DateTime.UtcNow,
                    Language = Language.ENGLISH
                }
            ],
            Descriptions = []
        };

        var project2 = new Project
        {
            Id = Guid.NewGuid(),
            StartDate = DateTime.UtcNow,
            SCIMId = "test-project-2",
            Titles = 
            [
                new ProjectTitle
                {
                    Id = Guid.NewGuid(),
                    Text = "Second Title",
                    Type = TitleType.Primary,
                    StartDate = DateTime.UtcNow,
                    Language = Language.ENGLISH
                }
            ],
            Descriptions = []
        };

        // Act
        string hash1 = project1.GetEmbeddingContentHash();
        string hash2 = project2.GetEmbeddingContentHash();

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void GetEmbeddingContentHash_WithEmptyProject_ReturnsValidHash()
    {
        // Arrange
        var project = new Project
        {
            Id = Guid.NewGuid(),
            StartDate = DateTime.UtcNow,
            SCIMId = "empty-project",
            Titles = [],
            Descriptions = []
        };

        // Act
        string hash = project.GetEmbeddingContentHash();

        // Assert
        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
        // Should be a valid hex string (SHA256 produces 64 hex characters)
        Assert.Equal(64, hash.Length);
        Assert.True(hash.All(c => "0123456789ABCDEF".Contains(c)));
    }

    [Fact]
    public void GetEmbeddingContentHash_IsConsistent_WithMultipleCalls()
    {
        // Arrange
        var project = new Project
        {
            Id = Guid.NewGuid(),
            StartDate = DateTime.UtcNow,
            SCIMId = "test-project",
            Titles = 
            [
                new ProjectTitle
                {
                    Id = Guid.NewGuid(),
                    Text = "Consistent Title",
                    Type = TitleType.Primary,
                    StartDate = DateTime.UtcNow,
                    Language = Language.ENGLISH
                }
            ],
            Descriptions = 
            [
                new ProjectDescription
                {
                    Id = Guid.NewGuid(),
                    Text = "Consistent description.",
                    Type = DescriptionType.Primary,
                    Language = Language.ENGLISH
                }
            ]
        };

        // Act
        string hash1 = project.GetEmbeddingContentHash();
        string hash2 = project.GetEmbeddingContentHash();
        string hash3 = project.GetEmbeddingContentHash();

        // Assert
        Assert.Equal(hash1, hash2);
        Assert.Equal(hash2, hash3);
    }

    [Theory]
    [InlineData("Short")]
    [InlineData("Medium length title with several words")]
    [InlineData("Very long title that contains many words and should still be processed correctly without any issues")]
    public void GetEmbeddingText_WithVariousTextLengths_HandlesCorrectly(string titleText)
    {
        // Arrange
        var project = new Project
        {
            Id = Guid.NewGuid(),
            StartDate = DateTime.UtcNow,
            SCIMId = "test-project",
            Titles = 
            [
                new ProjectTitle
                {
                    Id = Guid.NewGuid(),
                    Text = titleText,
                    Type = TitleType.Primary,
                    StartDate = DateTime.UtcNow,
                    Language = Language.ENGLISH
                }
            ],
            Descriptions = []
        };

        // Act
        string result = project.GetEmbeddingText();

        // Assert
        Assert.Equal(titleText, result);
    }

    [Fact]
    public void GetEmbeddingText_WithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        const string titleWithSpecialChars = "Prójëct wíth Spëcîál Châractërs & Émöjis 🚀";
        var project = new Project
        {
            Id = Guid.NewGuid(),
            StartDate = DateTime.UtcNow,
            SCIMId = "test-project",
            Titles = 
            [
                new ProjectTitle
                {
                    Id = Guid.NewGuid(),
                    Text = titleWithSpecialChars,
                    Type = TitleType.Primary,
                    StartDate = DateTime.UtcNow,
                    Language = Language.ENGLISH
                }
            ],
            Descriptions = []
        };

        // Act
        string result = project.GetEmbeddingText();

        // Assert
        Assert.Equal(titleWithSpecialChars, result);
        
        // Hash should also handle special characters correctly
        string hash = project.GetEmbeddingContentHash();
        Assert.NotNull(hash);
        Assert.Equal(64, hash.Length);
    }
}
