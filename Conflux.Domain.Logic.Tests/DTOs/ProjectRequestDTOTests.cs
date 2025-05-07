// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Conflux.Domain.Logic.DTOs;
using Xunit;

namespace Conflux.Domain.Logic.Tests.DTOs;

public class ProjectRequestDTOTests
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
        ProjectRequestDTO requestDTO = new()
        {
            Titles =
            [
                new()
                {
                    Text = "Title",
                    Type = TitleType.Primary,
                    StartDate = new(2021,
                        1,
                        1,
                        0,
                        0,
                        0,
                        DateTimeKind.Utc),
                },
            ],
            Descriptions =
            [
                new()
                {
                    Text = "Test Description",
                    Type = DescriptionType.Primary,
                    Language = Language.ENGLISH,
                },
            ],
            StartDate = new(2025,
                1,
                1,
                0,
                0,
                0,
                DateTimeKind.Utc),
            EndDate = new DateTime(2025,
                12,
                31,
                23,
                59,
                59,
                DateTimeKind.Utc),
            Id = Guid.NewGuid(),
        };

        // Act
        Project project = requestDTO.ToProject();

        // Assert
        Assert.NotNull(project);
        Assert.Single(project.Titles);
        Assert.Equal(requestDTO.Titles[0].Text, project.Titles[0].Text);
        Assert.Equal(requestDTO.Titles[0].Type, project.Titles[0].Type);
        Assert.Equal(requestDTO.Titles[0].StartDate, project.Titles[0].StartDate);
        Assert.Single(project.Descriptions);
        Assert.Equal(requestDTO.Descriptions[0].Text, project.Descriptions[0].Text);
        Assert.Equal(requestDTO.Descriptions[0].Language, project.Descriptions[0].Language);
        Assert.Equal(requestDTO.Descriptions[0].Text, project.Descriptions[0].Text);
        Assert.Equal(requestDTO.StartDate, project.StartDate);
        Assert.Equal(requestDTO.EndDate, project.EndDate);
        Assert.NotEqual(Guid.Empty, project.Id);
    }
}
