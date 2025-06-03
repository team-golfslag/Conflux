// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain;
using Conflux.Domain.Logic.DTOs.Requests;
using Conflux.Domain.Logic.DTOs.Responses;
using Conflux.Domain.Logic.Exceptions;
using Conflux.Domain.Logic.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Conflux.API.Tests.Controllers;

public class ProjectDescriptionsControllerTests
{
    private readonly ProjectDescriptionsController _controller;
    private readonly Mock<IProjectDescriptionsService> _mockService;

    public ProjectDescriptionsControllerTests()
    {
        _mockService = new();
        _controller = new(_mockService.Object);
    }

    [Fact]
    public async Task GetDescriptions_ReturnsListOfDescriptions()
    {
        // Arrange
        Guid projectId = Guid.NewGuid();
        List<ProjectDescriptionResponseDTO> descriptions =
        [
            new()
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Text = "Test Description",
                Type = DescriptionType.Primary,
                Language = Language.ENGLISH,
            },
            new()
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Text = "Another Description",
                Type = DescriptionType.Brief,
                Language = Language.ENGLISH,
            },
        ];

        _mockService.Setup(s => s.GetDescriptionsByProjectIdAsync(projectId))
            .ReturnsAsync(descriptions);

        // Act
        ActionResult<List<ProjectDescriptionResponseDTO>> result = await _controller.GetDescriptions(projectId);

        // Assert
        ActionResult<List<ProjectDescriptionResponseDTO>> actionResult =
            Assert.IsType<ActionResult<List<ProjectDescriptionResponseDTO>>>(result);
        List<ProjectDescriptionResponseDTO> returnValue =
            Assert.IsType<List<ProjectDescriptionResponseDTO>>(actionResult.Value);
        Assert.Equal(2, returnValue.Count);
        Assert.Contains(returnValue, d => d.Text == "Test Description");
        Assert.Contains(returnValue, d => d.Text == "Another Description");
    }

    [Fact]
    public async Task GetDescriptionById_WithValidIds_ReturnsDescription()
    {
        // Arrange
        Guid projectId = Guid.NewGuid();
        Guid descriptionId = Guid.NewGuid();
        ProjectDescriptionResponseDTO description = new()
        {
            Id = descriptionId,
            ProjectId = projectId,
            Text = "Test Description",
            Type = DescriptionType.Primary,
            Language = Language.ENGLISH,
        };

        _mockService.Setup(s => s.GetDescriptionByIdAsync(projectId, descriptionId))
            .ReturnsAsync(description);

        // Act
        ActionResult<ProjectDescriptionResponseDTO> result =
            await _controller.GetDescriptionById(projectId, descriptionId);

        // Assert
        ActionResult<ProjectDescriptionResponseDTO> actionResult =
            Assert.IsType<ActionResult<ProjectDescriptionResponseDTO>>(result);
        ProjectDescriptionResponseDTO returnValue = Assert.IsType<ProjectDescriptionResponseDTO>(actionResult.Value);
        Assert.Equal(descriptionId, returnValue.Id);
        Assert.Equal(projectId, returnValue.ProjectId);
        Assert.Equal("Test Description", returnValue.Text);
        Assert.Equal(DescriptionType.Primary, returnValue.Type);
    }

    [Fact]
    public async Task GetDescriptionById_WithInvalidIds_ThrowsProjectDescriptionNotFoundException()
    {
        // Arrange
        Guid projectId = Guid.NewGuid();
        Guid descriptionId = Guid.NewGuid();

        _mockService.Setup(s => s.GetDescriptionByIdAsync(projectId, descriptionId))
            .ThrowsAsync(new ProjectDescriptionNotFoundException(descriptionId));

        // Act & Assert
        await Assert.ThrowsAsync<ProjectDescriptionNotFoundException>(() =>
            _controller.GetDescriptionById(projectId, descriptionId));
    }

    [Fact]
    public async Task CreateDescription_ReturnsCreatedAtActionResult()
    {
        // Arrange
        Guid projectId = Guid.NewGuid();
        Guid descriptionId = Guid.NewGuid();
        ProjectDescriptionRequestDTO requestDto = new()
        {
            Text = "New Description",
            Type = DescriptionType.Primary,
            Language = Language.ENGLISH,
        };

        ProjectDescriptionResponseDTO createdDescription = new()
        {
            Id = descriptionId,
            ProjectId = projectId,
            Text = "New Description",
            Type = DescriptionType.Primary,
            Language = Language.ENGLISH,
        };

        _mockService.Setup(s => s.CreateDescriptionAsync(projectId, requestDto))
            .ReturnsAsync(createdDescription);

        // Act
        ActionResult<ProjectDescriptionResponseDTO> result = await _controller.CreateDescription(projectId, requestDto);

        // Assert
        ActionResult<ProjectDescriptionResponseDTO> actionResult =
            Assert.IsType<ActionResult<ProjectDescriptionResponseDTO>>(result);
        CreatedAtActionResult createdAtActionResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);

        Assert.Equal(nameof(ProjectDescriptionsController.GetDescriptionById), createdAtActionResult.ActionName);
        Assert.Equal(projectId, createdAtActionResult.RouteValues["projectId"]);
        Assert.Equal(descriptionId, createdAtActionResult.RouteValues["descriptionId"]);

        ProjectDescriptionResponseDTO returnValue =
            Assert.IsType<ProjectDescriptionResponseDTO>(createdAtActionResult.Value);
        Assert.Equal(descriptionId, returnValue.Id);
        Assert.Equal("New Description", returnValue.Text);
        Assert.Equal(DescriptionType.Primary, returnValue.Type);
    }

    [Fact]
    public async Task UpdateDescription_WithValidIds_ReturnsUpdatedDescription()
    {
        // Arrange
        Guid projectId = Guid.NewGuid();
        Guid descriptionId = Guid.NewGuid();
        ProjectDescriptionRequestDTO requestDto = new()
        {
            Text = "Updated Description",
            Type = DescriptionType.Brief,
            Language = Language.DUTCH,
        };

        ProjectDescriptionResponseDTO updatedDescription = new()
        {
            Id = descriptionId,
            ProjectId = projectId,
            Text = "Updated Description",
            Type = DescriptionType.Brief,
            Language = Language.DUTCH,
        };

        _mockService.Setup(s => s.UpdateDescriptionAsync(projectId, descriptionId, requestDto))
            .ReturnsAsync(updatedDescription);

        // Act
        ActionResult<ProjectDescriptionResponseDTO> result =
            await _controller.UpdateDescription(projectId, descriptionId, requestDto);

        // Assert
        ActionResult<ProjectDescriptionResponseDTO> actionResult =
            Assert.IsType<ActionResult<ProjectDescriptionResponseDTO>>(result);
        ProjectDescriptionResponseDTO returnValue = Assert.IsType<ProjectDescriptionResponseDTO>(actionResult.Value);
        Assert.Equal(descriptionId, returnValue.Id);
        Assert.Equal("Updated Description", returnValue.Text);
        Assert.Equal(DescriptionType.Brief, returnValue.Type);
        Assert.Equal(Language.DUTCH.Id, returnValue.Language.Id);
    }

    [Fact]
    public async Task UpdateDescription_WithInvalidIds_ThrowsProjectDescriptionNotFoundException()
    {
        // Arrange
        Guid projectId = Guid.NewGuid();
        Guid descriptionId = Guid.NewGuid();
        ProjectDescriptionRequestDTO requestDto = new()
        {
            Text = "Updated Description",
            Type = DescriptionType.Brief,
            Language = Language.DUTCH,
        };

        _mockService.Setup(s => s.UpdateDescriptionAsync(projectId, descriptionId, requestDto))
            .ThrowsAsync(new ProjectDescriptionNotFoundException(descriptionId));

        // Act & Assert
        await Assert.ThrowsAsync<ProjectDescriptionNotFoundException>(() =>
            _controller.UpdateDescription(projectId, descriptionId, requestDto));
    }

    [Fact]
    public async Task DeleteDescription_WithValidIds_ReturnsNoContent()
    {
        // Arrange
        Guid projectId = Guid.NewGuid();
        Guid descriptionId = Guid.NewGuid();

        _mockService.Setup(s => s.DeleteDescriptionAsync(projectId, descriptionId))
            .Returns(Task.CompletedTask);

        IActionResult result = await _controller.DeleteDescription(projectId, descriptionId);
        
        Assert.IsType<NoContentResult>(result);
    }
}
