// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Text.Json.Serialization;

namespace Conflux.Domain.Logic.DTOs;

/// <summary>
/// The Data Transfer Object for <see cref="Project" /> with GET.
/// </summary>
#pragma warning disable S101 // Types should be named in camel case
public class ProjectGetDTO
#pragma warning restore S101
{
    public Guid Id { get; init; }
    public string? SRAMId { get; init; }

    [JsonPropertyName("raid_id")]
    public string? RAiDId { get; init; }

    public required string Title { get; set; }
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<User> People { get; set; } = [];
    public List<Product> Products { get; set; } = [];
    public List<Party> Parties { get; set; } = [];
    public List<Role> Roles { get; set; } = [];

    /// <summary>
    /// Converts a <see cref="Project" /> to a <see cref="ProjectGetDTO" />
    /// </summary>
    /// <returns>The converted <see cref="ProjectGetDTO" /></returns>
    public static ProjectGetDTO FromProject(Project project, List<Role> roles) =>
        new()
        {
            Id = project.Id,
            SRAMId = project.SCIMId,
            RAiDId = project.RAiDId,
            Title = project.Title,
            Description = project.Description,
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            People = project.Users,
            Products = project.Products,
            Parties = project.Parties,
            Roles = roles,
        };
}
