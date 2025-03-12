using Conflux.Core.DTOs;
using Conflux.Domain;
using Xunit;

namespace Conflux.API.Tests;

public class ProjectDTOTests
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
        ProjectDTO dto = new() 
        {
            Title = "Test Project",
            Description = "Test Description",
            StartDate = new DateOnly(2025, 1, 1),
            EndDate = new DateOnly(2025, 12, 31),
        };

        // Act
        Project project = dto.ToProject();

        // Assert
        Assert.NotNull(project);
        Assert.Equal(dto.Title, project.Title);
        Assert.Equal(dto.Description, project.Description);
        Assert.Equal(dto.StartDate, project.StartDate);
        Assert.Equal(dto.EndDate, project.EndDate);
        Assert.NotEqual(Guid.Empty, project.Id);
    }
}
