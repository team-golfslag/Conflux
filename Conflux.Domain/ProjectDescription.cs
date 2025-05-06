// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Conflux.Domain;

public enum DescriptionType
{
    /// <summary>
    /// Primary, i.e., a preferred full description or abstract
    /// </summary>
    Primary = 318,

    /// <summary>
    /// Alternative, i.e., an additional or supplementary full description or abstract
    /// </summary>
    Alternative = 319,

    /// <summary>
    /// Brief, i.e., a shorted version of the primary description
    /// </summary>
    Brief = 3,

    /// <summary>
    /// Significance Statement
    /// </summary>
    Significance = 9,

    /// <summary>
    /// Methods
    /// </summary>
    Methods = 8,

    /// <summary>
    /// Objectives
    /// </summary>
    Objectives = 7,

    /// <summary>
    /// Acknowledgements, i.e., for recognition of people not listed as Contributors or organisations not listed as
    /// Organisations
    /// </summary>
    Acknowledgements = 392,

    /// <summary>
    /// Other, i.e., any other descriptive information such as a note
    /// </summary>
    Other = 6,
}

public class ProjectDescription
{
    [Key] public Guid Id { get; init; }
    [ForeignKey(nameof(Project))] public Guid ProjectId { get; init; }

    // TODO: What is a character?
    /// <summary>
    /// For raid may only be 1000 characters
    /// </summary>
    public required string Text { get; set; }

    public DescriptionType Type { get; init; }

    public Language? Language { get; set; }

    public string TypeSchemaUri => "https://vocabulary.raid.org/description.type.schema/320";
    public string TypeUri => $"https://vocabulary.raid.org/description.type.schema/{(int)Type}";
}
