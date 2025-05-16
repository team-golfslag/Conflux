// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Conflux.Domain;

public enum TitleType
{
    /// <summary>
    /// Primary, i.e., a preferred full or long title
    /// </summary>
    Primary = 5,

    /// <summary>
    /// Short
    /// </summary>
    Short = 157,

    /// <summary>
    /// Acronym
    /// </summary>
    Acronym = 156,

    /// <summary>
    /// Alternative, including subtitle or other supplemental title
    /// </summary>
    Alternative = 4,
}

public class ProjectTitle
{
    [Key] public Guid Id { get; init; }
    [ForeignKey(nameof(Project))] public Guid ProjectId { get; init; }

    // TODO: Should have a max length of 100
    public required string Text { get; set; }

    public Language? Language { get; set; }

    public required TitleType Type { get; init; }

    public DateTime StartDate { get; init; }
    public DateTime? EndDate { get; set; }

    public string TypeSchemaUri => "https://vocabulary.raid.org/title.type.schema/376";
    public string TypeUri => $"https://vocabulary.raid.org/title.type.schema/{(int)Type}";
}
