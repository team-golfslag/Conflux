// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.ComponentModel.DataAnnotations;

namespace Conflux.Domain;

public record GroupIdConnection
{
    [Key] public string Id { get; init; }

    [Required] public string Urn { get; init; }
}
