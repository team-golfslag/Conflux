// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Domain.Logic.DTOs;

/// <summary>
/// The Data Transfer Object for patching a <see cref="Contributor" />
/// </summary>
#pragma warning disable S101 // Types should be named in camel case
public class ContributorPatchDto
#pragma warning restore S101
{
    public string? Name { get; init; }
}
