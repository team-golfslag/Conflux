// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Text.Json.Serialization;

namespace Conflux.Domain.Logic.DTOs.Response;

public class ProjectResponseDTO
{
    public Guid Id { get; init; }

    [JsonPropertyName("raid_info")] public RAiDInfo? RAiDInfo { get; init; }

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public List<User> Users { get; set; } = [];

    public List<ContributorResponseDTO> Contributors { get; set; } = [];

    public List<Product> Products { get; set; } = [];

    public List<ProjectOrganisationResponseDTO> ProjectOrganisations { get; set; } = [];

    public List<ProjectTitle> Titles { get; set; } = [];

    public List<ProjectDescription> Descriptions { get; set; } = [];

    public DateTime LastestEdit { get; set; } = DateTime.UtcNow;
}

