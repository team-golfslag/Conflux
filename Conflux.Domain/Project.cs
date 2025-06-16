// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Pgvector;

namespace Conflux.Domain;

/// <summary>
/// Represents a project.
/// </summary>
public class Project
{
    [Key] public Guid Id { get; init; }

    [JsonPropertyName("scim_id")] public string? SCIMId { get; init; }

    [JsonPropertyName("raid_info")] public RAiDInfo? RAiDInfo { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public List<User> Users { get; set; } = [];

    public List<Contributor> Contributors { get; set; } = [];

    public List<Product> Products { get; set; } = [];

    public List<ProjectOrganisation> Organisations { get; set; } = [];

    public List<ProjectTitle> Titles { get; set; } = [];

    public List<ProjectDescription> Descriptions { get; set; } = [];

    public DateTime LastestEdit { get; set; } = DateTime.UtcNow;

    public string? Lectorate { get; set; }
    
    /// <summary>
    /// Owner organisation of the SRAM CO
    /// </summary>
    public string? OwnerOrganisation { get; set; }

    /// <summary>
    /// Semantic embedding vector for multilingual search (384 dimensions for all-MiniLM-L12-v2 model)
    /// </summary>
    [Column(TypeName = "vector(384)")]
    public Vector? Embedding { get; set; }

    /// <summary>
    /// Hash of the content used to generate the embedding to detect when re-embedding is needed
    /// </summary>
    public string? EmbeddingContentHash { get; set; }

    /// <summary>
    /// When the embedding was last updated
    /// </summary>
    public DateTime? EmbeddingLastUpdated { get; set; }
}
