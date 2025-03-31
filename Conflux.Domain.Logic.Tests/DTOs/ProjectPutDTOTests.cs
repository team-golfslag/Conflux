using Conflux.Domain.Logic.DTOs;
using Xunit;

namespace Conflux.Domain.Logic.Tests.DTOs;

public class ProjectPutDTOTests
{
    /// <summary>
    /// Given a valid ProjectDTO with Title, Description, StartDate, and EndDate,
    /// When the ToProject() method is called,
    /// Then a new Project is created with a new Id and the same Title, Description, StartDate, and EndDate.
    /// </summary>
    [Fact]
    public void ToProject_ShouldConvertDTOToProject()
    {
        // Arrange
        ProjectPostDTO postDto = new()
        {
            Id = Guid.NewGuid(),
            Title = "Test Project",
            Description = "Test Description",
            StartDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc),
        };

        // Act
        Project project = postDto.ToProject();

        // Assert
        Assert.NotNull(project);
        Assert.NotEqual(postDto.Id, project.Id);
        Assert.Equal(postDto.Title, project.Title);
        Assert.Equal(postDto.Description, project.Description);
        Assert.Equal(postDto.StartDate, project.StartDate);
        Assert.Equal(postDto.EndDate, project.EndDate);
        Assert.NotEqual(Guid.Empty, project.Id);
    }
}
