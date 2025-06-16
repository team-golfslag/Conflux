// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Data;
using Conflux.Domain;
using Conflux.Domain.Logic.DTOs.Requests;
using Conflux.Domain.Logic.DTOs.Responses;
using Conflux.Domain.Logic.Exceptions;
using Conflux.Domain.Logic.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Conflux.Domain.Logic.Tests.Services;

public class ProjectDescriptionsServiceTests : IDisposable
{
    private readonly ConfluxContext _context;
    private readonly ProjectDescriptionsService _service;
    private readonly Mock<IProjectsService> _mockProjectsService;
    private readonly Mock<ILogger<ProjectDescriptionsService>> _mockLogger;

    public ProjectDescriptionsServiceTests()
    {
        ServiceProvider serviceProvider = new ServiceCollection()
            .AddEntityFrameworkInMemoryDatabase()
            .BuildServiceProvider();

        DbContextOptions<ConfluxContext> options = new DbContextOptionsBuilder<ConfluxContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.CreateVersion7()}")
            .UseInternalServiceProvider(serviceProvider)
            .Options;

        ConfluxContext context = new(options);
        context.Database.EnsureCreated();
        _context = context;
        
        // Create mocks for dependencies
        _mockProjectsService = new Mock<IProjectsService>();
        _mockLogger = new Mock<ILogger<ProjectDescriptionsService>>();
        
        _service = new(_context, _mockProjectsService.Object, _mockLogger.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetDescriptionsByProjectIdAsync_ShouldThrow_WhenProjectDoesNotExist()
    {
        // Arrange
        Guid nonExistentProjectId = Guid.CreateVersion7();

        // Act & Assert
        await Assert.ThrowsAsync<ProjectNotFoundException>(async () =>
            await _service.GetDescriptionsByProjectIdAsync(nonExistentProjectId));
    }

    [Fact]
    public async Task GetDescriptionsByProjectIdAsync_ShouldReturnAllDescriptions()
    {
        // Arrange
        Project project = await SetupTestProject();

        // Act
        List<ProjectDescriptionResponseDTO> result = await _service.GetDescriptionsByProjectIdAsync(project.Id);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains(result, d => d.Type == DescriptionType.Brief);
        Assert.Contains(result, d => d.Type == DescriptionType.Primary);
        Assert.Contains(result, d => d.Type == DescriptionType.Other);
    }

    [Fact]
    public async Task GetDescriptionsByProjectIdAsync_ShouldReturnEmptyList_WhenNoDescriptions()
    {
        // Arrange
        Project project = new()
        {
            Id = Guid.CreateVersion7(),
            SCIMId = null,
            RAiDInfo = null,
            StartDate = DateTime.UtcNow.Date,
            EndDate = null,
            Users = new List<User>(),
            Contributors = new List<Contributor>(),
            Products = new List<Product>(),
            Organisations = new List<ProjectOrganisation>(),
            Titles = new List<ProjectTitle>(),
            Descriptions = new List<ProjectDescription>(),
            LastestEdit = DateTime.UtcNow,
        };

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        // Act
        List<ProjectDescriptionResponseDTO> result = await _service.GetDescriptionsByProjectIdAsync(project.Id);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetDescriptionByIdAsync_ShouldThrow_WhenDescriptionDoesNotExist()
    {
        // Arrange
        Project project = await SetupTestProject();
        Guid nonExistentDescriptionId = Guid.CreateVersion7();

        // Act & Assert
        await Assert.ThrowsAsync<ProjectDescriptionNotFoundException>(async () =>
            await _service.GetDescriptionByIdAsync(project.Id, nonExistentDescriptionId));
    }

    [Fact]
    public async Task GetDescriptionByIdAsync_ShouldReturnDescription_WhenDescriptionExists()
    {
        // Arrange
        Project project = await SetupTestProject();
        ProjectDescription expectedDescription = project.Descriptions[0];

        // Act
        ProjectDescriptionResponseDTO result = await _service.GetDescriptionByIdAsync(project.Id, expectedDescription.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedDescription.Id, result.Id);
        Assert.Equal(expectedDescription.Text, result.Text);
        Assert.Equal(expectedDescription.Type, result.Type);
        Assert.Equal(expectedDescription.Language ?? Language.DUTCH, result.Language);
    }

    [Fact]
    public async Task CreateDescriptionAsync_ShouldThrow_WhenProjectDoesNotExist()
    {
        // Arrange
        Guid nonExistentProjectId = Guid.CreateVersion7();
        ProjectDescriptionRequestDTO dto = new()
        {
            Text = "Test description",
            Type = DescriptionType.Brief,
            Language = Language.ENGLISH
        };

        // Act & Assert
        await Assert.ThrowsAsync<ProjectNotFoundException>(async () =>
            await _service.CreateDescriptionAsync(nonExistentProjectId, dto));
    }

    [Fact]
    public async Task CreateDescriptionAsync_ShouldCreateDescription_WhenValidRequest()
    {
        // Arrange
        Project project = await SetupTestProject();
        ProjectDescriptionRequestDTO dto = new()
        {
            Text = "New test description",
            Type = DescriptionType.Brief,
            Language = Language.ENGLISH
        };

        // Act
        ProjectDescriptionResponseDTO result = await _service.CreateDescriptionAsync(project.Id, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.Text, result.Text);
        Assert.Equal(dto.Type, result.Type);
        Assert.Equal(dto.Language, result.Language);
        Assert.Equal(project.Id, result.ProjectId);

        // Verify description was saved to database
        ProjectDescription? savedDescription = await _context.ProjectDescriptions.FindAsync(result.Id);
        Assert.NotNull(savedDescription);
        Assert.Equal(dto.Text, savedDescription.Text);

        // Verify embedding update was triggered
        _mockProjectsService.Verify(x => x.UpdateProjectEmbeddingAsync(project.Id), Times.Once);
    }

    [Fact]
    public async Task CreateDescriptionAsync_ShouldUseDefaultLanguage_WhenLanguageNotSpecified()
    {
        // Arrange
        Project project = await SetupTestProject();
        ProjectDescriptionRequestDTO dto = new()
        {
            Text = "Test description without language",
            Type = DescriptionType.Other,
            Language = null
        };

        // Act
        ProjectDescriptionResponseDTO result = await _service.CreateDescriptionAsync(project.Id, dto);

        // Assert
        Assert.Equal(Language.DUTCH, result.Language);
    }

    [Fact]
    public async Task UpdateDescriptionAsync_ShouldThrow_WhenDescriptionDoesNotExist()
    {
        // Arrange
        Project project = await SetupTestProject();
        Guid nonExistentDescriptionId = Guid.CreateVersion7();
        ProjectDescriptionRequestDTO dto = new()
        {
            Text = "Updated description",
            Type = DescriptionType.Brief,
            Language = Language.ENGLISH
        };

        // Act & Assert
        await Assert.ThrowsAsync<ProjectDescriptionNotFoundException>(async () =>
            await _service.UpdateDescriptionAsync(project.Id, nonExistentDescriptionId, dto));
    }

    [Fact]
    public async Task UpdateDescriptionAsync_ShouldUpdateDescription_WhenValidRequest()
    {
        // Arrange
        Project project = await SetupTestProject();
        ProjectDescription descriptionToUpdate = project.Descriptions[0];
        ProjectDescriptionRequestDTO dto = new()
        {
            Text = "Updated description text",
            Type = DescriptionType.Primary,
            Language = Language.DUTCH
        };

        // Act
        ProjectDescriptionResponseDTO result = await _service.UpdateDescriptionAsync(project.Id, descriptionToUpdate.Id, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.Text, result.Text);
        Assert.Equal(dto.Type, result.Type);
        Assert.Equal(dto.Language, result.Language);
        Assert.Equal(descriptionToUpdate.Id, result.Id);

        // Verify description was updated in database
        ProjectDescription? updatedDescription = await _context.ProjectDescriptions.FindAsync(descriptionToUpdate.Id);
        Assert.NotNull(updatedDescription);
        Assert.Equal(dto.Text, updatedDescription.Text);
        Assert.Equal(dto.Type, updatedDescription.Type);
        Assert.Equal(dto.Language, updatedDescription.Language);

        // Note: UpdateProjectEmbeddingAsync is called in a background task, so we can't verify it immediately
    }

    [Fact]
    public async Task DeleteDescriptionAsync_ShouldThrow_WhenDescriptionDoesNotExist()
    {
        // Arrange
        Project project = await SetupTestProject();
        Guid nonExistentDescriptionId = Guid.CreateVersion7();

        // Act & Assert
        await Assert.ThrowsAsync<ProjectDescriptionNotFoundException>(async () =>
            await _service.DeleteDescriptionAsync(project.Id, nonExistentDescriptionId));
    }

    [Fact]
    public async Task DeleteDescriptionAsync_ShouldDeleteDescription_WhenValidRequest()
    {
        // Arrange
        Project project = await SetupTestProject();
        ProjectDescription descriptionToDelete = project.Descriptions[0];
        Guid descriptionId = descriptionToDelete.Id;

        // Act
        await _service.DeleteDescriptionAsync(project.Id, descriptionId);

        // Assert
        // Verify description was deleted from database
        ProjectDescription? deletedDescription = await _context.ProjectDescriptions.FindAsync(descriptionId);
        Assert.Null(deletedDescription);

        // Verify embedding update was triggered
        _mockProjectsService.Verify(x => x.UpdateProjectEmbeddingAsync(project.Id), Times.Once);
    }

    [Theory]
    [InlineData(DescriptionType.Brief)]
    [InlineData(DescriptionType.Primary)]
    [InlineData(DescriptionType.Other)]
    public async Task CreateDescriptionAsync_ShouldHandleAllDescriptionTypes(DescriptionType descriptionType)
    {
        // Arrange
        Project project = await SetupTestProject();
        ProjectDescriptionRequestDTO dto = new()
        {
            Text = $"Test {descriptionType} description",
            Type = descriptionType,
            Language = Language.ENGLISH
        };

        // Act
        ProjectDescriptionResponseDTO result = await _service.CreateDescriptionAsync(project.Id, dto);

        // Assert
        Assert.Equal(descriptionType, result.Type);
        Assert.Equal($"Test {descriptionType} description", result.Text);
    }

    [Theory]
    [InlineData("nld")]
    [InlineData("eng")]
    public async Task CreateDescriptionAsync_ShouldHandleAllLanguages(string languageId)
    {
        // Arrange
        Project project = await SetupTestProject();
        Language language = languageId == "nld" ? Language.DUTCH : Language.ENGLISH;
        ProjectDescriptionRequestDTO dto = new()
        {
            Text = $"Test description in {language.Id}",
            Type = DescriptionType.Brief,
            Language = language
        };

        // Act
        ProjectDescriptionResponseDTO result = await _service.CreateDescriptionAsync(project.Id, dto);

        // Assert
        Assert.Equal(language, result.Language);
    }

    [Fact]
    public async Task CreateDescriptionAsync_ShouldHandleEmbeddingUpdateFailure()
    {
        // Arrange
        Project project = await SetupTestProject();
        ProjectDescriptionRequestDTO dto = new()
        {
            Text = "Test description",
            Type = DescriptionType.Brief,
            Language = Language.ENGLISH
        };

        // Setup mock to throw exception
        _mockProjectsService.Setup(x => x.UpdateProjectEmbeddingAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new Exception("Embedding update failed"));

        // Act - Should not throw despite embedding update failure
        ProjectDescriptionResponseDTO result = await _service.CreateDescriptionAsync(project.Id, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.Text, result.Text);

        // Give time for the background task to complete
        await Task.Delay(100);

        // Verify that the embedding update was attempted
        _mockProjectsService.Verify(x => x.UpdateProjectEmbeddingAsync(project.Id), Times.Once);
    }

    [Fact]
    public async Task UpdateDescriptionAsync_ShouldHandleEmbeddingUpdateFailure()
    {
        // Arrange
        Project project = await SetupTestProject();
        ProjectDescription descriptionToUpdate = project.Descriptions[0];
        ProjectDescriptionRequestDTO dto = new()
        {
            Text = "Updated description",
            Type = DescriptionType.Primary,
            Language = Language.DUTCH
        };

        // Setup mock to throw exception
        _mockProjectsService.Setup(x => x.UpdateProjectEmbeddingAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new Exception("Embedding update failed"));

        // Act - Should not throw despite embedding update failure
        ProjectDescriptionResponseDTO result = await _service.UpdateDescriptionAsync(project.Id, descriptionToUpdate.Id, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.Text, result.Text);

        // Give time for the background task to complete
        await Task.Delay(100);

        // Verify that the embedding update was attempted
        _mockProjectsService.Verify(x => x.UpdateProjectEmbeddingAsync(project.Id), Times.Once);
    }

    [Fact]
    public async Task DeleteDescriptionAsync_ShouldHandleEmbeddingUpdateFailure()
    {
        // Arrange
        Project project = await SetupTestProject();
        ProjectDescription descriptionToDelete = project.Descriptions[0];

        // Setup mock to throw exception
        _mockProjectsService.Setup(x => x.UpdateProjectEmbeddingAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new Exception("Embedding update failed"));

        // Act - Should not throw despite embedding update failure
        await _service.DeleteDescriptionAsync(project.Id, descriptionToDelete.Id);

        // Assert
        ProjectDescription? deletedDescription = await _context.ProjectDescriptions.FindAsync(descriptionToDelete.Id);
        Assert.Null(deletedDescription);

        // Give time for the background task to complete
        await Task.Delay(100);

        // Verify that the embedding update was attempted
        _mockProjectsService.Verify(x => x.UpdateProjectEmbeddingAsync(project.Id), Times.Once);
    }

    private async Task<Project> SetupTestProject()
    {
        Guid projectId = Guid.CreateVersion7();
        Project project = new()
        {
            Id = projectId,
            SCIMId = null,
            RAiDInfo = null,
            StartDate = DateTime.UtcNow.Date,
            EndDate = null,
            Users = new List<User>(),
            Contributors = new List<Contributor>(),
            Products = new List<Product>(),
            Organisations = new List<ProjectOrganisation>(),
            Titles = new List<ProjectTitle>(),
            Descriptions = new List<ProjectDescription>
            {
                new()
                {
                    Id = Guid.CreateVersion7(),
                    ProjectId = projectId,
                    Text = "Brief test description",
                    Type = DescriptionType.Brief,
                    Language = Language.ENGLISH,
                },
                new()
                {
                    Id = Guid.CreateVersion7(),
                    ProjectId = projectId,
                    Text = "Primary test description with much more detail about the project",
                    Type = DescriptionType.Primary,
                    Language = Language.DUTCH,
                },
                new()
                {
                    Id = Guid.CreateVersion7(),
                    ProjectId = projectId,
                    Text = "Other test description",
                    Type = DescriptionType.Other,
                    Language = null, // Test null language
                },
            },
            LastestEdit = DateTime.UtcNow,
        };

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        return project;
    }
}
