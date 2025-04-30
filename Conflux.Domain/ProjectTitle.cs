// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Conflux.Domain;

public enum TitleType
{
    /// <summary>
    /// Primary, i.e., a preferred full or long title
    /// </summary>
    Primary = 380,

    /// <summary>
    /// Short
    /// </summary>
    Short = 381,

    /// <summary>
    /// Acronym
    /// </summary>
    Acronym = 378,

    /// <summary>
    /// Alternative, including subtitle or other supplemental title
    /// </summary>
    Alternative = 379,
}

public class ProjectTitle
{
    [Key] public Guid Id { get; init; }
    [ForeignKey(nameof(Project))] public Guid ProjectId { get; init; }

    // TODO: Should have a max length of 100
    public required string Text { get; init; }

    [MaxLength(3)] public string? Language { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required TitleType Type { get; init; }

    public DateTime StartDate { get; init; }
    public DateTime? EndDate { get; init; }

    public string TypeSchemaUri => "https://vocabulary.raid.org/title.type.schema/376";
    public string LanguageSchemaUri => "https://www.iso.org/standard/74575.html";
    public string TypeUri => $"https://vocabulary.raid.org/title.type.id/{(int)Type}";
}
