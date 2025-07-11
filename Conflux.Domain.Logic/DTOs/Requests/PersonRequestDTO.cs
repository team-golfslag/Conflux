// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.ComponentModel.DataAnnotations;

namespace Conflux.Domain.Logic.DTOs.Requests;

public class PersonRequestDTO
{
    [Required] public required string Name { get; init; }
    public string? GivenName { get; init; }
    public string? FamilyName { get; init; }
    public string? Email { get; init; }
    public string? ORCiD { get; init; }
}
