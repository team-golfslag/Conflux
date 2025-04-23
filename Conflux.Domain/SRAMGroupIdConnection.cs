// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.ComponentModel.DataAnnotations;

namespace Conflux.Domain;

public record SRAMGroupIdConnection
{
    [Required] public required string Id { get; init; }

    [Key] public required string Urn { get; init; }
}
