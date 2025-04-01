// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.ComponentModel.DataAnnotations;

namespace Conflux.Domain;

/// <summary>
/// Represents a person.
/// </summary>
public class Person
{
    [Key] public Guid Id { get; set; }

    [Required] public required string Name { get; set; }
}
