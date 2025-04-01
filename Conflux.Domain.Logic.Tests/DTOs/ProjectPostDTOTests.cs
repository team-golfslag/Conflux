// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

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
            StartDate = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2021, 12, 31, 23, 59, 59, DateTimeKind.Utc),
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
