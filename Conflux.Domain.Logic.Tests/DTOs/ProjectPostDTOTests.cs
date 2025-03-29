using Conflux.Domain.Logic.DTOs;
using Xunit;

namespace Conflux.Domain.Logic.Tests.DTOs;

public class ProjectPostDTOTests
{
    /// <summary>
    /// Given a valid ProjectPostDTO,
    /// When ToProject() is called,
    /// Then a new Project is created with a new Id and the same Title, Description, StartDate, and EndDate.
    /// </summary>
    [Fact]
    public void ToProject_ReturnsProject()
    {
        // Arrange
        ProjectPostDTO projectPostDTO = new()
        {
            Title = "Title",
            Description = "Description",
            StartDate = new DateOnly(2021, 1, 1),
            EndDate = new DateOnly(2021, 12, 31),
        };

        // Act
        Project project = projectPostDTO.ToProject();

        // Assert
        Assert.NotNull(project);
        Assert.Equal(projectPostDTO.Title, project.Title);
        Assert.Equal(projectPostDTO.Description, project.Description);
        Assert.Equal(projectPostDTO.StartDate, project.StartDate);
        Assert.Equal(projectPostDTO.EndDate, project.EndDate);
        Assert.NotEqual(Guid.Empty, project.Id);
    }
}
